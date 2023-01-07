module internal IniLib.Parser

open System.Text.RegularExpressions
open IniLib.Utilities

let internal escapeCodeToCharacter = dict [
    '\\', '\\'
    '\'', '\''
    '"', '"'
    '#', '#'
    ':', ':'
    ' ', ' '
    's', ' '
    '0', char 0
    'a', char 7
    'b', char 8
    'f', char 12
    'n', char 10
    'r', char 13
    't', char 9
    'v', char 11
]

let parse (options: Options) tokens =
    /// Marks text and whitespace tokens in the interior of a KeyNameNode, KeyValueNode or SectionHeadingNode as replaceable,
    /// excluding any whitespace or non-text tokens at the beginning or end of the list.
    let rec markReplaceableNodes node =
        /// Marks all text and whitespace tokens starting with the first text token, and returns the list in reverse.
        let rec markMiddleToEnd doMark out nodes =
            match doMark, nodes with
            | _, [] ->
                out

            | false, TokenNode (LeftBracket _ as token)::rest ->
                markMiddleToEnd false ((TokenNode token) :: out) rest

            | false, TokenNode (Whitespace _ as token)::rest ->
                markMiddleToEnd false ((TriviaNode token) :: out) rest

            | _, (TokenNode token)::rest
            | _, (ReplaceableTokenNode token)::rest ->
                markMiddleToEnd true ((ReplaceableTokenNode token) :: out) rest

            | b, node::rest ->
                markMiddleToEnd b (node :: out) rest

        /// Unmarks non-text tokens at the beginning of the reversed list, and returns the list in the original order,
        /// with all whitespace tokens at both ends unmarked.
        let rec unmarkBeginning doUnmark out nodes =
            match doUnmark, nodes with
            | _, [] ->
                out

            | _, ReplaceableTokenNode (RightBracket _ as token)::rest ->
                unmarkBeginning false (TokenNode token :: out) rest

            | true, TokenNode (Whitespace _ as token)::rest
            | true, ReplaceableTokenNode (Whitespace _ as token)::rest ->
                unmarkBeginning true (TriviaNode token :: out) rest

            | true, node::rest ->
                unmarkBeginning false (node :: out) rest

            | b, node::rest ->
                unmarkBeginning b (node :: out) rest

        let mark constructor children =
            children
            |> markMiddleToEnd false []
            |> unmarkBeginning true []
            |> constructor

        match node with
        | KeyNameNode (name, children) ->
            mark (Operators.tuple2 name >> KeyNameNode) children
        | KeyValueNode (name, children) ->
            mark (Operators.tuple2 name >> KeyValueNode) children
        | SectionHeadingNode (name, children) ->
            mark (Operators.tuple2 name >> SectionHeadingNode) children
        | _ ->
            failwithf "Expected KeyNameNode, KeyValueNode or SectionHeadingNode, got %A" node

    let parseComment tokens =
        let rec parseComment' text consumedTokens tokens =
            match tokens with
            | (Whitespace (t, _, _) as token)::rest
            | (Comment (t, _, _) as token)::rest ->
                let nextText = text + t

                if nextText.EndsWith("\n") then
                    let nextNode = CommentNode (nextText.Trim(), List.rev (TokenNode token :: consumedTokens))
                    Some nextNode, rest
                else
                    parseComment' nextText (TokenNode token :: consumedTokens) rest

            | _ ->
                let nextNode = CommentNode (text.Trim(), List.rev consumedTokens)
                Some nextNode, tokens

        let rec parseEpilogue consumedTokens tokens =
            match tokens with
            | (Whitespace (text, _, _) as ws)::rest when not (text.EndsWith("\n")) ->
                parseEpilogue (TokenNode ws :: consumedTokens) rest
            | (CommentIndicator _ as t)::rest ->
                true, (TokenNode t :: consumedTokens), rest
            | _ ->
                false, consumedTokens, tokens

        let hasComment, consumedTokens, rest = parseEpilogue [] tokens

        if hasComment then
            parseComment' "" consumedTokens rest
        else
            let consumedTokens = List.map (function TokenNode token -> token) consumedTokens
            None, consumedTokens @ rest

    let parseKey leadingWhitespace tokens =
        let expected () =
            match options.nameValueDelimiterRule with
            | EqualsDelimiter -> "'='"
            | ColonDelimiter -> "':'"
            | EqualsOrColonDelimiter -> "'=' or ':'"
            | NoDelimiter -> "value"

        let (|EscapedText|_|) token =
            match token with
            | EscapedChar (c, line, column) -> Some (string escapeCodeToCharacter[c], line, column)
            | EscapedUnicodeChar (codepoint, line, column) -> Some (codepoint |> char |> string, line, column)
            | LineContinuation (line, column) -> Some (options.newlineRule.toText(), line, column)
            | _ -> None

        let rec parseKeyName name quote consumedTokens tokens =
            match name, tokens with
            // Premature newline
            | _, (Whitespace (text, _, _) as textToken)::_
            | _, (Text (text, _, _) as textToken)::_ when text.EndsWith("\n") ->
                failwithf "Unexpected end of line reading key at %O" (Token.endPosition textToken)

            // No name yet - consume whitespace
            | None, (Whitespace _ as whitespaceToken)::rest ->
                parseKeyName None quote (TokenNode whitespaceToken :: consumedTokens) rest

            // No name yet - got quote
            | None, (Quote _ as quoteToken)::rest ->
                parseKeyName (Some "") (Some quoteToken) (TokenNode quoteToken :: consumedTokens) rest

            // Set the name
            | None, (Text (t, _, _) as textToken)::rest ->
                parseKeyName (Some t) quote (TokenNode textToken :: consumedTokens) rest

            // Set the name with escaped text
            | None, (EscapedText (text, _, _) as token::rest) ->
                parseKeyName (Some text) quote (TokenNode token :: consumedTokens) rest

            // When no delimiter: whitespace terminates key name
            | (Some name), (Whitespace _)::_ when quote = None && options.nameValueDelimiterRule = NameValueDelimiterRule.NoDelimiter ->
                let name = name.Trim()
                let keyNameNode = KeyNameNode (name, List.rev consumedTokens)
                name, keyNameNode, tokens

            // Append whitespace and text to the already read name
            | (Some name1), (Text (name2, _, _) as textToken)::rest
            | (Some name1), (Whitespace (name2, _, _) as textToken)::rest ->
                let value = name1 + name2
                parseKeyName (Some value) quote (TokenNode textToken :: consumedTokens) rest

            // Append escaped text to already read name
            | (Some name), (EscapedText (text, _, _) as token::rest) ->
                let value = name + text
                parseKeyName (Some value) quote (TokenNode token :: consumedTokens) rest

            // Closing quote
            | (Some name), (Quote _ as quoteToken)::rest when quote <> None ->
                // Consume rest of whitespace
                let whitespace =
                    rest
                    |> List.takeWhile (function Whitespace _ -> true | _ -> false)
                    |> List.map TriviaNode
                let rest = rest[whitespace.Length..]

                let keyNameNode = KeyNameNode (name, whitespace @ TokenNode quoteToken :: consumedTokens |> List.rev)
                name, keyNameNode, rest

            // Assignment token - we're done
            | (Some name), (Assignment _)::_ ->
                let name = name.Trim()
                let keyNameNode = KeyNameNode (name, List.rev consumedTokens)
                name, keyNameNode, tokens

            | None, token::_ ->
                failwithf $"Expected key name, got {token} at {Token.position token}"

            | (Some _), token::_ ->
                failwithf $"Expected {expected()}, got {token} at {Token.position token}"

            | _, [] ->
                failwith $"Ran out of input reading key name at {Node.endPosition consumedTokens[0]}"

        let matchAssignment tokens =
            match options.nameValueDelimiterRule, tokens with
            | NoDelimiter, _ -> [], tokens
            | _, (Assignment _ as assignmentToken)::rest -> [TokenNode assignmentToken], rest
            | _, token::_ -> failwithf "Expected assignment, got %O at %O" token (Token.position token)
            | _ -> failwithf "Expected %s, got end of input" (expected ())
        
        let rec parseKeyValue value quote consumedTokens keyName input =
            /// Consume a text token and continue if there is more input on the line, or produce a KeyValueNode
            let inline matchValueText (text: string) quote textToken rest =
                let inline terminate() =
                    let keyValue = if quote = None then text.Trim() else text
                    let keyValueNode = KeyValueNode (keyValue, List.rev (TokenNode textToken :: consumedTokens))
                    keyValue, keyValueNode, rest

                // Terminate if out of input, or if value ends in unescaped newline or quote
                match rest, textToken with
                | [], _
                | _, Quote _ ->
                    terminate()

                | _, Text (text, _, _)
                | _, Whitespace (text, _, _) when quote = None && text.EndsWith("\n") ->
                    terminate()

                | _ ->
                    parseKeyValue (Some text) quote (TokenNode textToken :: consumedTokens) keyName rest

            // Consumes text from a Text or Whitespace token. Ends if the text ends in a newline, otherwise continues parsing.
            match value, input with
            // Consume whitespace until value starts
            | None, (Whitespace _ as whitespaceToken)::rest when quote = None ->
                parseKeyValue None quote (TokenNode whitespaceToken :: consumedTokens) keyName rest

            // Match initial quotation mark
            | None, (Quote _ as quoteToken)::rest when options.quotationRule >= UseQuotation && quote = None ->
                parseKeyValue (Some "") (Some quoteToken) (TokenNode quoteToken :: consumedTokens) keyName rest

            // Set value
            | None, (Text (text, _, _) as textToken)::rest
            | None, (EscapedText (text, _, _) as textToken)::rest ->
                matchValueText text quote textToken rest

            // Append unescaped text to value
            | (Some value), (Text (text, _, _) as textToken)::rest
            | (Some value), (Whitespace (text, _, _) as textToken)::rest ->
                let value = value + text
                matchValueText value quote textToken rest

            // Append escaped text to value
            | (Some value), (EscapedText (text, _, _) as textToken)::rest ->
                let value = value + text
                matchValueText value quote textToken rest

            // Hit a comment - we're done
            | _, (CommentIndicator _)::_ ->
                let value = Option.defaultValue "" value
                let keyValue = value.Trim()
                let keyValueNode = KeyValueNode (keyValue, List.rev consumedTokens)
                keyValue, keyValueNode, input

            // Add left bracket, right bracket, and assignment tokens verbatim
            | (Some value), (LeftBracket _ as token)::rest
            | (Some value), (RightBracket _ as token)::rest
            | (Some value), (Assignment _ as token)::rest ->
                let text = value + Token.toText options token
                parseKeyValue (Some text) quote (TokenNode token::consumedTokens) keyName rest

            // Closing quote terminates the value
            | (Some value), (Quote _ as quoteToken)::rest when options.quotationRule >= UseQuotation ->
                matchValueText value quote quoteToken rest

            | _, token::_ ->
                failwithf $"Unexpected {token} at {Token.position token} while reading key '{keyName}'"

            | _ ->
                let lastPosition = consumedTokens |> List.head |> Node.position
                failwith $"Ran out of input reading key value at {lastPosition}"

        let keyName, keyNameNode, tokens = parseKeyName None None leadingWhitespace tokens
        let assignment, tokens = matchAssignment tokens
        let literalKeyValue, keyValueNode, tokens = parseKeyValue None None [] keyName tokens
        let keyValue =
            if options.quotationRule >= UseQuotation
                && literalKeyValue.StartsWith("\"")
                && literalKeyValue.EndsWith("\"") then
                literalKeyValue.Substring(1, literalKeyValue.Length - 2)
            else
                literalKeyValue

        let endsWithNewline =
            keyValueNode
            |> Node.getChildren
            |> List.tryLast
            |> Option.map (Node.toText options >> String.endsWith "\n")
            |> Option.defaultValue false

        let comment, tokens =
            if endsWithNewline then None, tokens
            else parseComment tokens

        let keyNodeChildren =
            [ markReplaceableNodes keyNameNode ]
            @ assignment
            @ [ markReplaceableNodes keyValueNode ]
            @ Option.toList comment

        let keyNode = KeyNode (keyName, keyValue, keyNodeChildren)

        keyName, keyNode, tokens

    let rec parseSectionHeading parsedSections sectionName consumedTokens tokens =
        match tokens with
        | (Text (text, _, _) as textToken)::rest
        | (Whitespace (text, _, _) as textToken)::rest ->
            let consumedTokens = TokenNode textToken :: consumedTokens
            parseSectionHeading parsedSections (sectionName + text) consumedTokens rest

        | (RightBracket _ as bracketToken)::rest ->
            let sectionName = sectionName.Trim()

            if options.duplicateSectionRule = DisallowDuplicateSections && Set.contains sectionName parsedSections then
                failwith $"Duplicate section '{sectionName}' at %O{Node.position consumedTokens[0]}"

            // Consume remaining whitespace until end of line to create the SectionHeadingNode
            let nextLineIndex =
                rest
                |> List.findIndex (function
                    | Whitespace (text, _, _) when text.EndsWith("\n") -> true
                    | Whitespace _ -> false
                    | t -> failwith $"Expected whitespace or newline at {Token.position t}, got {t}")

            let whitespace = List.map TriviaNode rest[..nextLineIndex]
            let rest = rest[nextLineIndex + 1..]
            let children = (List.rev consumedTokens) @ [TokenNode bracketToken] @ whitespace
            let headingNode = markReplaceableNodes (SectionHeadingNode (sectionName, children))

            headingNode, sectionName, rest

    let rec parseKeys parsedKeys outNodes tokens =
        match tokens with
        // No more output or next section - done
        | [] 
        | (LeftBracket _)::_ ->
            List.rev outNodes, tokens

        // Consume whitespace and add trivia node
        | Whitespace (text, _, _) as whitespaceToken::rest ->
            let triviaNode = TriviaNode whitespaceToken
            parseKeys parsedKeys (triviaNode :: outNodes) rest

        // Comment
        | (CommentIndicator _)::_ ->
            let (Some nextComment), tokens = parseComment tokens
            parseKeys parsedKeys (nextComment :: outNodes) tokens

        // Parse the next key with any consumed whitespace from the last line of the output added to it
        | _ ->
            let doesNotEndWithNewline = Node.toText options >> String.contains "\n" >> not
            let leadingWhitespace = List.ofSeq (Seq.takeWhile doesNotEndWithNewline outNodes)
            let outNodes = List.skip (List.length leadingWhitespace) outNodes
            let keyName, nextKey, tokens = parseKey leadingWhitespace tokens

            let outNodes =
                match options.duplicateKeyRule with
                | DisallowDuplicateKeys when Set.contains keyName parsedKeys ->
                    failwith $"Duplicate key '{keyName}' at %O{Node.position nextKey}"

                | _ ->
                    nextKey :: outNodes

            parseKeys (Set.add keyName parsedKeys) outNodes tokens

    let mergeSections (SectionNode (name, existingChildren)) (SectionNode (_, duplicateChildren) as duplicateSection) =
        let first, whitespace =
            match options.duplicateSectionRule with
            | MergeOriginalSectionIntoDuplicate ->
                let out =
                    existingChildren
                    |> List.rev
                    |> List.skipWhile Node.isWhitespace
                    |> List.rev
                out, []
            | MergeDuplicateSectionIntoOriginal ->
                let revChildren = List.rev existingChildren
                let whitespace = List.takeWhile Node.isWhitespace revChildren
                let out =
                    revChildren
                    |> List.skipWhile Node.isWhitespace
                    |> List.rev
                out, whitespace
            | _ ->
                failwith $"mergeSections shouldn't have been called with {options.duplicateSectionRule} at {Node.position duplicateSection}"

        let second =
            match options.duplicateSectionRule with
            | MergeOriginalSectionIntoDuplicate ->
                List.filter (function SectionHeadingNode _ -> false | _ -> true) duplicateChildren
            | MergeDuplicateSectionIntoOriginal ->
                duplicateChildren
                |> List.rev
                |> List.skipWhile Node.isWhitespace
                |> List.rev
                |> List.filter (function SectionHeadingNode _ -> false | _ -> true)

        SectionNode (name, first @ second @ whitespace)

    let rec parse' parsedSections output tokens =
        let RE_WHITESPACE_CLUSTERS = new Regex("\s+")

        match tokens with
        // Done
        | [] ->
            List.rev output

        // Consume whitespace and add trivia node
        | (Whitespace _) as whitespaceToken::rest ->
            let triviaNode = TriviaNode whitespaceToken
            parse' parsedSections (triviaNode :: output) rest

        // Comment
        | (CommentIndicator _)::_ ->
            let (Some nextNode), rest = parseComment tokens
            parse' parsedSections (nextNode :: output) rest

        // Section with a heading
        | (LeftBracket _ as bracketToken)::rest ->
            let headingNode, sectionName, tokens = parseSectionHeading parsedSections "" [TokenNode bracketToken] rest
            let sectionName = RE_WHITESPACE_CLUSTERS.Replace(sectionName, " ")
            let keys, tokens = parseKeys Set.empty [] tokens
            let sectionNode = SectionNode (sectionName, headingNode :: keys)
            let existingSectionIndex = List.tryFindIndex (function SectionNode (name, _) when name = sectionName -> true | _ -> false) output

            let output =
                match existingSectionIndex, options.duplicateSectionRule with
                | Some i, MergeOriginalSectionIntoDuplicate ->
                    let mergedSection = mergeSections output[i] sectionNode
                    mergedSection :: List.deleteAt i output

                | Some i, MergeDuplicateSectionIntoOriginal ->
                    let mergedSection = mergeSections output[i] sectionNode
                    List.replace output[i] mergedSection output

                | _ ->
                    sectionNode :: output

            parse' (Set.add sectionName parsedSections) output tokens

        // Property outside section (i.e. global property)
        | (Quote _)::_
        | (Text _)::_ when options.globalKeysRule = AllowGlobalKeys ->
            let keys, tokens = parseKeys Set.empty [] tokens
            let section = SectionNode ("<global>", keys)
            parse' parsedSections (section :: output) tokens

        | token::_ ->
            failwith $"Expected section, got %O{token} at %O{Token.position token}"

    RootNode (parse' Set.empty [] tokens)
