﻿module internal IniLib.NodeBuilder

open System.Text.RegularExpressions
open IniLib.Utilities

let private RE_WHITESPACE = new Regex("\\s")
let private RE_ESCAPE_CHARACTERS = new Regex(@"[\b\f\n\r\t\v]" +                // Control characters
                                             @"|(?<!\\)[""'#:;= ]" +            // Unescaped special characters
                                             @"|\\(?![\b\f\n\r\t\vx""'#:;= ])") // Unescaped slash

let inline newlineTrivia options = TriviaNode (Whitespace (options.newlineRule.toText(), PositionUndetermined))
let inline replaceableText text = ReplaceableTokenNode (Text (text, PositionUndetermined))
let spaceTrivia = TriviaNode (Whitespace (" ", PositionUndetermined))

let addNewlineIfNeeded options node =
    if String.endsWith "\n" (Node.toText options node) then
        node
    else
        Node.appendChild (newlineTrivia options) node

let private escapes =
    Parser.escapeCodeToCharacter
    |> List.ofSeq
    |> List.map (fun kvp -> kvp.Key, kvp.Value)
    |> List.except [('s', ' ')]

let private escape text =
    List.fold
        (fun text (escapeCode, escapedChar) -> String.replace (string escapedChar) $"\\{escapeCode}" text)
        text
        escapes

let private escapeNode constructor node text children =
    match text with
    | RegexMatch RE_ESCAPE_CHARACTERS _ ->
        let prologue = children |> List.takeWhile Node.isNotReplaceable
        let epilogue = children |> List.rev |> List.takeWhile Node.isNotReplaceable |> List.rev
        let nameText = [ replaceableText (escape text) ]
        constructor (text, prologue @ nameText @ epilogue)
    | _ ->
        node

let private quoteNode constructor node text children =
    if List.exists (function ReplaceableTokenNode (Quote _) -> true | _ -> false) children then
        node
    else
        let prologue = children |> List.takeWhile Node.isNotReplaceable
        let nameText = children |> List.filter Node.isReplaceable
        let epilogue = children |> List.rev |> List.takeWhile Node.isNotReplaceable |> List.rev
        let quote = [ ReplaceableTokenNode (Quote PositionUndetermined) ]
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

    match node with
    | KeyValueNode _ when options.quotationRule = AlwaysUseQuotation ->
        quoteNode constructor node text children
    | _ when options.quotationRule >= UseQuotation && hasWhitespace ->
        quoteNode constructor node text children
    | _ when options.escapeSequenceRule >= UseEscapeSequences && hasWhitespace ->
        escapeNode constructor node text children
    | _ ->
        node

let keyName options name =
    let whitespace =
        match options.nameValueDelimiterSpacingRule with
        | LeftOnly | BothSides -> [ spaceTrivia ]
        | _ -> []

    KeyNameNode (name, replaceableText name :: whitespace)
    |> sanitize options

let keyValue options value =
    let whitespace =
        match options.nameValueDelimiterSpacingRule with
        | RightOnly | BothSides -> [ spaceTrivia ]
        | _ -> []
    let children = [
        whitespace
        [ replaceableText value ]
    ]

    KeyValueNode (value, List.collect id children)
    |> sanitize options

let key options name value =
    let assignmentToken =
        match options.nameValueDelimiterPreferenceRule with
        | PreferEqualsDelimiter -> [ TokenNode (Assignment ('=', PositionUndetermined)) ]
        | PreferColonDelimiter -> [ TokenNode (Assignment (':', PositionUndetermined)) ]
        | PreferNoDelimiter -> []

    let children =
        [ keyName options name ]
        @ assignmentToken
        @ [ keyValue options value ]
        @ [ newlineTrivia options ]
        
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
                    TokenNode (LeftBracket PositionUndetermined)
                    replaceableText name
                    TokenNode (RightBracket PositionUndetermined)
                    newlineTrivia options
                ]) ]
            let newline = [ newlineTrivia options ]
            sectionHeadingNode, newline

    SectionNode (name, sectionHeading @ children @ trailingNewline)

let comment options text =
    let commentIndicator =
        match options.commentRule with
        | HashComments | HashAndSemicolonComments -> '#'
        | SemicolonComments -> ';'
    let children =
        [ TokenNode (CommentIndicator (commentIndicator, PositionUndetermined))
          TriviaNode (Whitespace (" ", PositionUndetermined))
          ReplaceableTokenNode (Text (text, PositionUndetermined)) ]
    CommentNode (text, children)
