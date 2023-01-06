module internal IniLib.NodeBuilder

open System.Text.RegularExpressions
open IniLib.Utilities

let private RE_WHITESPACE = new Regex("\\s");
let private RE_ESCAPE_CHARACTERS = new Regex("[\a\b\f\n\r\t\v\"\'\\#: ]")

let private escapes =
    Parser.escapeCodeToCharacter
    |> List.ofSeq
    |> List.map (fun kvp -> kvp.Key, kvp.Value)
    |> List.except [('s', ' ')]

let private escape s =
    (s, escapes)
    ||> List.fold (fun s (escapeCode, escapedChar) -> String.replace (string escapedChar) ("\\" + string escapeCode) s)

let private escapeNode constructor node text children =
    if RE_ESCAPE_CHARACTERS.IsMatch(text) then
        let prologue = children |> List.takeWhile Node.isNotReplaceable
        let epilogue = children |> List.rev |> List.takeWhile Node.isNotReplaceable |> List.rev
        let nameText = [ ReplaceableTokenNode (Text (escape text, 0, 0)) ]
        constructor (text, prologue @ nameText @ epilogue)
    else
        node

let private quoteNode constructor node text children =
    match children with
    | ReplaceableTokenNode (Quote _) :: _ :: ReplaceableTokenNode (Quote _) :: _ ->
        node

    | _ ->
        let prologue = children |> List.takeWhile Node.isNotReplaceable
        let nameText = children |> List.filter Node.isReplaceable
        let epilogue = children |> List.rev |> List.takeWhile Node.isNotReplaceable |> List.rev
        let quote = [ ReplaceableTokenNode (Quote (0, 0)) ]
        constructor (text, prologue @ quote @ nameText @ quote @ epilogue)

/// Sanitizes the key name or value to prevent a parsing error. If the text contains a whitespace,
/// it is wrapped in quotation marks if the quotation rule is UseQuotation or AlwaysUseQuotation,
/// and it is escaped if the escape sequence rule is UseEscapeSequences or UseEscapeSequencesAndLineContinuation.
let sanitize options node =
    let constructor, text, children =
        match node with
        | KeyNameNode (text, children) -> KeyNameNode, text, children
        | KeyValueNode (text, children) -> KeyValueNode, text, children
        | _ -> failwith $"Expected KeyNameNode or KeyValueNode, got %O{node}"

    let hasWhitespace = RE_WHITESPACE.IsMatch(text)
    
    let maybeQuotedNode =
        match options.quotationRule, node with
        | AlwaysUseQuotation, KeyValueNode _ ->
            quoteNode constructor node text children
        | AlwaysUseQuotation, _ | UseQuotation, _ when hasWhitespace ->
            quoteNode constructor node text children
        | _ ->
            node

    match options.escapeSequenceRule with
    | UseEscapeSequencesAndLineContinuation | UseEscapeSequences when hasWhitespace ->
        escapeNode constructor maybeQuotedNode text children
    | _ ->
        maybeQuotedNode

let keyName options name =
    let whitespace =
        match options.nameValueDelimiterSpacingRule with
        | LeftOnly
        | BothSides -> [ TriviaNode (Whitespace (" ", 0, 0)) ]
        | _ -> []

    KeyNameNode (name, [ ReplaceableTokenNode (Text (name, 0, 0)) ] @ whitespace)
    |> sanitize options

let keyValue options value =
    let whitespace =
        match options.nameValueDelimiterSpacingRule with
        | RightOnly
        | BothSides -> [TriviaNode (Whitespace (" ", 0, 0))]
        | _ -> []
    let children = [
        whitespace
        [ ReplaceableTokenNode (Text (value, 0, 0)) ]
        [ TriviaNode (Text (options.newlineRule.toText(), 0, 0)) ]
    ]

    KeyValueNode (value, List.collect id children)
    |> sanitize options

let key options name value =
    let assignmentToken =
        match options.nameValueDelimiterPreferenceRule with
        | PreferEqualsDelimiter -> [ TokenNode (Assignment ('=', 0, 0)) ]
        | PreferColonDelimiter -> [ TokenNode (Assignment (':', 0, 0)) ]
        | PreferNoDelimiter -> []

    let children =
        [ keyName options name ]
        @ assignmentToken
        @ [ keyValue options value ]

    KeyNode (name, value, children)
