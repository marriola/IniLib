module internal IniLib.NodeBuilder

open System.Text.RegularExpressions
open IniLib.Utilities

let private RE_WHITESPACE = new Regex("\\s")
let private RE_ESCAPE_CHARACTERS = new Regex(@"[\b\f\n\r\t\v""'\\#: ]")

let inline newlineTrivia options = TriviaNode (Whitespace (options.newlineRule.toText(), 0, 0))
let inline whitespaceTrivia space = TriviaNode (Whitespace (space, 0, 0))
let inline replaceableText text = ReplaceableTokenNode (Text (text, 0, 0))

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
        let nameText = [ replaceableText (escape text) ]
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
        | LeftOnly | BothSides -> [ whitespaceTrivia " " ]
        | _ -> []

    KeyNameNode (name, replaceableText name :: whitespace)
    |> sanitize options

let keyValue options value =
    let whitespace =
        match options.nameValueDelimiterSpacingRule with
        | RightOnly | BothSides -> [ whitespaceTrivia " "]
        | _ -> []
    let children = [
        whitespace
        [ replaceableText value ]
        [ newlineTrivia options ]
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

let keys options keys =
    keys
    |> List.map (fun (name, value) -> key options name value)

let section options name children =
    let sectionHeading, trailingNewline =
        if options.globalKeysRule = AllowGlobalKeys && name = "<global>" then
            [], []
        else
            let sectionHeadingNode =
                [ SectionHeadingNode (name, [
                    TokenNode (LeftBracket (0, 0))
                    replaceableText name
                    TokenNode (RightBracket (0, 0))
                    newlineTrivia options
                ]) ]
            let newline = [ newlineTrivia options ]
            sectionHeadingNode, newline

    SectionNode (name, sectionHeading @ children @ trailingNewline)
