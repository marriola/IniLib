module internal IniLib.Parser

open System.Text.RegularExpressions
open IniLib.Utilities

let internal escapeCodeToCharacter = dict [
    '\\', '\\'
    '\'', '\''
    '"', '"'
    '#', '#'
    ':', ':'
    ';', ';'
    '=', '='
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

type private ParseKeyPartState =
    { input: Token list
      text: string option
      lastQuote: Token option
      isQuoted: bool
      consumedTokens: Node list }
with
    static member defaultFor input = { input = input; text = None; lastQuote = None; isQuoted = false; consumedTokens = [] }

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

    let tryParseComment tokens =
        let rec tryParseComment' text consumedTokens tokens =
            match tokens with
            | (Whitespace (t, _) as token)::rest
            | (Comment (t, _) as token)::rest ->
                let nextText = text + t

                if nextText.EndsWith "\n" then
                    let nextNode = CommentNode (nextText.Trim(), List.rev (TriviaNode token :: consumedTokens))
                    Some nextNode, rest
                else
                    tryParseComment' nextText (ReplaceableTokenNode token :: consumedTokens) rest

            | _ ->
                let nextNode = CommentNode (text.Trim(), List.rev consumedTokens)
                Some nextNode, tokens

        // Check the rest of the line to see if it has a comment
        let rec parseEpilogue consumedTokens tokens =
            match tokens with
            | (Whitespace (text, _) as ws)::rest when not (text.EndsWith "\n") ->
                parseEpilogue (TriviaNode ws :: consumedTokens) rest
            | (CommentIndicator _ as t)::rest ->
                true, (TokenNode t :: consumedTokens), rest
            | _ ->
                false, consumedTokens, tokens

        let hasComment, consumedTokens, rest = parseEpilogue [] tokens

        if hasComment then
            tryParseComment' "" consumedTokens rest
        else
            let consumedTokens = List.map (function TriviaNode token -> token) consumedTokens
            None, consumedTokens @ rest

    let parseKey leadingWhitespace tokens =
        let expectedAfterName =
            match options.nameValueDelimiterRule with
            | EqualsDelimiter -> "'='"
            | ColonDelimiter -> "':'"
            | EqualsOrColonDelimiter -> "'=' or ':'"
            | NoDelimiter -> "value"

        let (|EscapedText|_|) token =
            match token with
            | EscapedChar (c, position) ->
                if not (escapeCodeToCharacter.ContainsKey c) then
                    failwith $"Invalid escape sequence \\{c} at {position}"
                Some (string escapeCodeToCharacter[c], position)
            | EscapedUnicodeChar (codepoint, position) ->
                Some (codepoint |> char |> string, position)
            | LineContinuation position ->
                Some (options.newlineRule.toText(), position)
            | _ -> None

        let rec parseKeyName state =
            match state.text, state.input with
            // No name yet - consume whitespace
            | None, (Whitespace _ as whitespaceToken)::rest ->
                parseKeyName { state with
                                    input = rest
                                    consumedTokens = TriviaNode whitespaceToken :: state.consumedTokens }

            // No name yet - got quote
            | None, (Quote _ as quoteToken)::rest ->
                parseKeyName { state with
                                    input = rest
                                    text = Some ""
                                    lastQuote = Some quoteToken
                                    consumedTokens = TokenNode quoteToken :: state.consumedTokens }

            // Set the name
            | None, (Text (text, _) as textToken)::rest
            | None, (EscapedText (text, _) as textToken::rest) ->
                parseKeyName { state with
                                    input = rest
                                    text = Some text
                                    consumedTokens = TokenNode textToken :: state.consumedTokens }

            // When no delimiter: whitespace terminates key name
            | (Some name), (Whitespace _)::_ when state.lastQuote = None && options.nameValueDelimiterRule = NameValueDelimiterRule.NoDelimiter ->
                let name = name.Trim()
                let keyNameNode = KeyNameNode (name, List.rev state.consumedTokens)
                name, keyNameNode, state.input

            | (Some _), (Whitespace _ as whitespaceToken)::_ when Token.endsWith options "\n" whitespaceToken ->
                failwith $"Expected text or {expectedAfterName}, got end of line at {Token.position whitespaceToken}"

            // Append escaped text to the already read name
            | (Some name), (EscapedText (text, _) as textToken)::rest ->
                let name = if state.isQuoted then name else name + text
                parseKeyName { state with
                                    input = rest
                                    text = Some name
                                    consumedTokens = TokenNode textToken :: state.consumedTokens }

            // Append whitespace and text to the already read name
            | (Some name), (Text (text, _) as textToken)::rest
            | (Some name), (Whitespace (text, _) as textToken)::rest ->
                let name = if state.isQuoted then name else name + text
                parseKeyName { state with
                                    input = rest
                                    text = Some name
                                    consumedTokens = TokenNode textToken :: state.consumedTokens }

            // Append brackets and assignment verbatim when quoted
            | (Some name1), (LeftBracket _ as token)::rest
            | (Some name1), (RightBracket _ as token)::rest
            | (Some name1), (Assignment _ as token)::rest when state.lastQuote <> None ->
                let textToken = Token.toTextToken token

                parseKeyName { state with
                                    input = rest
                                    text = Some (name1 + Token.toText options textToken)
                                    consumedTokens = TokenNode textToken :: state.consumedTokens }

            // Closing quote
            | (Some name), (Quote _ as quoteToken)::rest when state.lastQuote <> None ->
                parseKeyName { state with
                                    input = rest
                                    text = Some name
                                    lastQuote = None
                                    isQuoted = true
                                    consumedTokens = ReplaceableTokenNode quoteToken :: state.consumedTokens }

            // Assignment token - we're done
            | (Some name), (Assignment _)::_ ->
                let name = if state.isQuoted then name else name.Trim()
                let keyNameNode = KeyNameNode (name, List.rev state.consumedTokens)
                name, keyNameNode, state.input

            // Hit a non-text token - we're done as long as we're not expecting an assignment token
            | (Some name), _ when options.nameValueDelimiterRule = NoDelimiter ->
                let name = if state.isQuoted then name else name.Trim()
                let keyNameNode = KeyNameNode (name, List.rev state.consumedTokens)
                name, keyNameNode, state.input

            | None, token::_ ->
                failwithf $"Expected key name, got {token} at {Token.position token}"

            | (Some _), token::_ ->
                failwithf $"Expected {expectedAfterName}, got {token} at {Token.position token}"

            | _, [] ->
                failwith $"Ran out of input reading key name at {Node.endPosition state.consumedTokens[0]}"

        let matchAssignment tokens =
            match options.nameValueDelimiterRule, tokens with
            | NoDelimiter, _ ->
                [], tokens
            | _, (Assignment _ as assignmentToken)::rest ->
                [TokenNode assignmentToken], rest
            | _, token::_ ->
                failwithf "Expected assignment, got %O at %O" token (Token.position token)
            | _ ->
                failwithf "Expected %s, got end of file" expectedAfterName

        let rec parseKeyValue state =
            // Don't consume any more input when terminating
            let doNotConsume = id

            // Consume the last text token when terminating
            let consumeNextToken (consumedTokens, input: Token list) =
                let nextToken::rest = input
                let consumedTokens = Node.ofToken nextToken :: consumedTokens
                consumedTokens, rest

            // Consume all whitespace up until the next newline
            let consumeWhitespace (consumedTokens, input: Token list) =
                let whitespace = List.takeWhile (function Whitespace (text, _) when text.EndsWith "\n" -> true | _ -> false) input
                let rest = input[whitespace.Length..]
                let consumedTokens = (whitespace |> List.rev |> List.map Node.ofToken) @ consumedTokens
                consumedTokens, rest

            /// Continue if there is more input on the line, or produce a KeyValueNode, optionally consuming some tokens and adding them to the key
            let inline matchValueText fConsume (text: string) =
                let inline terminate() =
                    let consumedTokens, nextInput = fConsume (state.consumedTokens, state.input)
                    let consumedTokens, trailingWhitespace = Node.splitTrailingWhitespace Operators.giveTrue (List.rev consumedTokens)
                    let keyValue = if state.lastQuote = None then text.Trim() else text
                    let keyValueNode = KeyValueNode (keyValue, consumedTokens)
                    trailingWhitespace, keyValue, keyValueNode, nextInput

                // Terminate if out of input, or if value ends in unescaped newline or quote
                match state.input with
                | []
                | _::[] ->
                    terminate()

                | Quote _::_ when state.lastQuote <> None ->
                    terminate()

                | Text (text, _)::_
                | Whitespace (text, _)::_ when state.lastQuote = None && text.EndsWith "\n" ->
                    terminate()

                | token::rest ->
                    parseKeyValue { state with
                                        input = rest
                                        text = if text.Length > 0 then Some text else None
                                        consumedTokens = TokenNode token :: state.consumedTokens }

            match state.text, state.input with
            // Empty value
            | None, [] ->
                matchValueText doNotConsume ""

            // Consume whitespace until value starts
            | None, (Whitespace _ as whitespaceToken)::rest when state.lastQuote = None ->
                matchValueText consumeNextToken ""

            // Match initial quotation mark
            | None, (Quote _ as quoteToken)::rest when options.quotationRule >= UseQuotation && state.lastQuote = None ->
                parseKeyValue { state with
                                    input = rest
                                    text = Some ""
                                    lastQuote = Some quoteToken
                                    consumedTokens = TokenNode quoteToken :: state.consumedTokens }

            // Set value
            | None, Text (text, _)::_
            | None, EscapedText (text, _)::_ ->
                matchValueText consumeNextToken text

            // Append text to value
            | (Some value), (Text (text, _) as token)::rest
            | (Some value), (Whitespace (text, _) as token)::rest
            | (Some value), (EscapedText (text, _) as token)::rest ->
                if state.lastQuote <> None && List.isEmpty rest then
                    failwith $"Expected '\"', got end of file at {Token.endPosition token}"

                let value = value + text
                matchValueText consumeNextToken value

            // Hit a comment - we're done
            | _, (CommentIndicator _)::_ when state.lastQuote = None ->
                let value = Option.defaultValue "" state.text
                let keyValue = value.Trim()
                let consumedTokens, trailingWhitespace = Node.splitTrailingWhitespace Operators.giveTrue (List.rev state.consumedTokens)
                let keyValueNode = KeyValueNode (keyValue, consumedTokens)
                trailingWhitespace, keyValue, keyValueNode, state.input

            // Add left bracket, right bracket, and assignment tokens verbatim
            | (Some value), (LeftBracket _ as token)::rest
            | (Some value), (RightBracket _ as token)::rest
            | (Some value), (Assignment _ as token)::rest ->
                if state.lastQuote <> None && List.isEmpty rest then
                    failwith $"Expected '\"', got end of file at {Token.endPosition token}"

                let textToken = Token.toTextToken token

                parseKeyValue { state with
                                    input = rest
                                    text = Some (value + Token.toText options textToken)
                                    consumedTokens = TokenNode textToken :: state.consumedTokens }

            // Closing quote terminates the value
            | (Some value), (Quote _)::_ when state.lastQuote <> None ->
                matchValueText (consumeNextToken >> consumeWhitespace) value

            | _, token::_ ->
                failwithf $"Unexpected {token} at {Token.position token} while reading key"

            | _ ->
                let lastPosition = state.consumedTokens |> List.head |> Node.position
                failwith $"Ran out of input reading key value at {lastPosition}"

        let keyName, keyNameNode, tokens = parseKeyName (ParseKeyPartState.defaultFor tokens)
        let assignment, tokens = matchAssignment tokens
        let trailingWhitespace, keyValue, keyValueNode, tokens = parseKeyValue (ParseKeyPartState.defaultFor tokens)

        let endsWithNewline =
            trailingWhitespace
            |> List.tryLast
            |> Option.map (Node.endsWith options "\n")
            |> Option.defaultValue false

        let comment, tokens =
            if endsWithNewline then None, tokens
            else tryParseComment tokens

        let comment, newline =
            match comment with
            | None -> None, None
            | Some comment ->
                let commentChildren = Node.getChildren comment
                let commentChildren, newline = Node.splitTrailingWhitespace (Node.endsWith options "\n") commentChildren
                let comment = CommentNode (Node.joinReplaceableText options commentChildren, commentChildren)
                Some comment, List.tryHead newline

        let keyNodeChildren =
            leadingWhitespace
            @ [ markReplaceableNodes keyNameNode ]
            @ assignment
            @ [ markReplaceableNodes keyValueNode ]
            @ trailingWhitespace
            @ Option.toList comment
            @ Option.toList newline

        let keyNode = KeyNode (keyName, keyValue, keyNodeChildren)

        keyName, keyNode, tokens

    let rec parseSectionHeading parsedSections sectionName consumedTokens tokens =
        match tokens with
        | Whitespace (whitespace, position)::_ when whitespace.EndsWith "\n" ->
            failwith $"Expected text or ']', got end of line at {position}"

        | (Text (text, _) as textToken)::rest
        | (Whitespace (text, _) as textToken)::rest ->
            let consumedTokens = TokenNode textToken :: consumedTokens
            parseSectionHeading parsedSections (sectionName + text) consumedTokens rest

        | (RightBracket _ as bracketToken)::rest ->
            let sectionName = sectionName.Trim()

            if options.duplicateSectionRule = DisallowDuplicateSections && Set.contains sectionName parsedSections then
                failwith $"Duplicate section '{sectionName}' at %O{Node.position consumedTokens[0]}"

            // Consume remaining whitespace until end of line to create the SectionHeadingNode
            let nextLineIndex =
                rest
                |> List.tryFindIndex (function
                    | Whitespace (text, _) when text.EndsWith "\n" -> true
                    | Whitespace _ -> false
                    | t -> failwith $"Expected whitespace or newline at {Token.position t}, got {t}")

            let rest, whitespace =
                match nextLineIndex with
                | Some nextLineIndex -> rest[nextLineIndex + 1..], List.map TriviaNode rest[..nextLineIndex]
                | None -> rest, []
            let children = (List.rev consumedTokens) @ [TokenNode bracketToken] @ whitespace
            let headingNode = markReplaceableNodes (SectionHeadingNode (sectionName, children))

            headingNode, sectionName, rest

        | [] ->
            failwith $"Expected text or ']', got end of file at {Node.endPosition consumedTokens[0]}"

        | token::_ ->
            failwith $"Expected text or ']', got {token} at {Token.position token}"

    let rec parseKeys parsedKeys outNodes tokens =
        let doesNotEndWithNewline = Node.toText options >> String.contains "\n" >> not

        match tokens with
        // No more output or next section - done
        | [] 
        | (LeftBracket _)::_ ->
            List.rev outNodes, tokens

        // Consume whitespace and add trivia node
        | Whitespace (text, _) as whitespaceToken::rest ->
            let triviaNode = TriviaNode whitespaceToken
            parseKeys parsedKeys (triviaNode :: outNodes) rest

        // Comment
        | (CommentIndicator _)::_ ->
            let (Some comment), tokens = tryParseComment tokens
            let leadingWhitespace =
                outNodes
                |> List.takeWhile Node.isWhitespace
                |> List.takeWhile (Node.endsWith options "\n" >> not)
                |> List.rev
            let comment = Node.prependChildren leadingWhitespace comment
            let nextOutNodes = comment :: outNodes[leadingWhitespace.Length..]
            parseKeys parsedKeys nextOutNodes tokens

        // Parse the next key with any consumed whitespace from the last line of the output added to it
        | _ ->
            // Validate that there is at least one newline between here and the last key
            let lastKeyIndex =
                outNodes
                |> List.tryFindIndex (function KeyNode _ -> true | _ -> false)
                |> Option.defaultValue -1
            let nodesBetweenKeys = outNodes[0..lastKeyIndex]
            if nodesBetweenKeys.Length > 0 && not (List.exists (Node.endsWith options "\n") nodesBetweenKeys) then
                failwith $"Expected end of line, got key at {Node.endPosition outNodes[0]}"

            let leadingWhitespace = List.ofSeq (Seq.takeWhile (fun n -> Node.isWhitespace n && doesNotEndWithNewline n) outNodes)
            let outNodes = List.skip (List.length leadingWhitespace) outNodes
            let keyName, nextKey, tokens = parseKey leadingWhitespace tokens

            let outNodes =
                match options.duplicateKeyRule with
                | DisallowDuplicateKeys when Set.contains keyName parsedKeys ->
                    failwith $"Duplicate key '{keyName}' at {Node.position nextKey}"

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
            let (Some nextNode), rest = tryParseComment tokens
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
        | (Quote _)::_ | (Text _)::_ when options.globalKeysRule = AllowGlobalKeys ->
            let keys, tokens = parseKeys Set.empty [] tokens
            let section = SectionNode ("<global>", keys)
            parse' parsedSections (section :: output) tokens

        | token::_ ->
            failwith $"Expected section, got %O{token} at %O{Token.position token}"

    RootNode (parse' Set.empty [] tokens)
