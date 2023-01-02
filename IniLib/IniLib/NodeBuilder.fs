module internal IniLib.NodeBuilder

open System.Text.RegularExpressions
open IniLib.Utilities

let private RE_LEADING_OR_TRAILING_WHITESPACE = new Regex(["\\s+.*"; ".*?\\s+"; "\\s+.*?\\s+"] |> String.concat "|")
let private RE_WHITESPACE = new Regex("\\s");

let keyValueText options value =
    let quote =
        match options.quotationRule with
        | AlwaysUseQuotation ->
            Some (ReplaceableTokenNode (Quote (0, 0)))
        | UseQuotation when RE_LEADING_OR_TRAILING_WHITESPACE.IsMatch(value) ->
            Some (ReplaceableTokenNode (Quote (0, 0)))
        | _ ->
            None
    [ quote
      Some (ReplaceableTokenNode (Text (value, 0, 0)))
      quote ]
    |> List.choose id

let private escapes =
    Parser.escapeCodeToCharacter
    |> List.ofSeq
    |> List.map (fun kvp -> kvp.Key, kvp.Value)
    |> List.except [('s', ' ')]

let private escape s =
    (s, escapes)
    ||> List.fold (fun s (escapeCode, escapedChar) -> String.replace (string escapedChar) ("\\" + string escapeCode) s)

let private escapeNode (KeyNameNode (name, children) as keyNameNode) =
    let prologue = children |> List.takeWhile (function ReplaceableTokenNode _ -> false | _ -> true)
    let epilogue = children |> List.rev |> List.takeWhile (function ReplaceableTokenNode _ -> false | _ -> true) |> List.rev
    let nameText = ReplaceableTokenNode (Text (escape name, 0, 0))
    KeyNameNode (name, prologue @ [nameText] @ epilogue)

let private quoteNode (KeyNameNode (name, children) as keyNameNode) =
    let prologue = children |> List.takeWhile (function ReplaceableTokenNode _ -> false | _ -> true)
    let nameText = children |> List.filter (function ReplaceableTokenNode _ -> true | _ -> false)
    let epilogue = children |> List.rev |> List.takeWhile (function ReplaceableTokenNode _ -> false | _ -> true) |> List.rev
    let quote = [ ReplaceableTokenNode (Quote (0, 0)) ]
    KeyNameNode (name, prologue @ quote @ nameText @ quote @ epilogue)

/// Sanitizes the key name when using the NoDelimiter rule. If the key name contains whitespace, it is either quoted or escaped,
/// depending on the quotation rule and escape sequence rule chosen.
let sanitize options (KeyNameNode (name, _) as keyNameNode) =
    let hasWhitespace = RE_WHITESPACE.IsMatch(name)
    match options with
    | { nameValueDelimiterRule = NoDelimiter; quotationRule = AlwaysUseQuotation } -> quoteNode keyNameNode
    | { nameValueDelimiterRule = NoDelimiter; quotationRule = UseQuotation } when hasWhitespace -> quoteNode keyNameNode
    | { nameValueDelimiterRule = NoDelimiter; escapeSequenceRule = UseEscapeSequencesAndLineContinuation }
    | { nameValueDelimiterRule = NoDelimiter; escapeSequenceRule = UseEscapeSequences } when hasWhitespace -> escapeNode keyNameNode
    | _ -> keyNameNode

let key options name value =
    let keyName name =
        let whitespace =
            match options.nameValueDelimiterSpacingRule with
            | LeftOnly
            | BothSides -> [ TriviaNode (Whitespace (" ", 0, 0)) ]
            | _ -> []
        KeyNameNode (name, [ ReplaceableTokenNode (Text (name, 0, 0)) ] @ whitespace)

    let keyValue value =
        let whitespace =
            match options.nameValueDelimiterSpacingRule with
            | RightOnly
            | BothSides -> [TriviaNode (Whitespace (" ", 0, 0))]
            | _ -> []
        let children = [
            whitespace
            keyValueText options value
            [TriviaNode (Text (options.newlineRule.toText(), 0, 0))]
        ]
        KeyValueNode (value, List.collect id children)

    let assignmentToken =
        match options.nameValueDelimiterRule with
        | EqualsDelimiter -> Some (TokenNode (Assignment ('=', 0, 0)))
        | ColonDelimiter -> Some (TokenNode (Assignment (':', 0, 0)))
        | NoDelimiter -> None

    let children = [
        Some (sanitize options (keyName name))
        assignmentToken
        Some (keyValue value)
    ]

    KeyNode (name, value, List.choose id children)
