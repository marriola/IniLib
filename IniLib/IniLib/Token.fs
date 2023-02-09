namespace IniLib

open IniLib.Utilities

type Token =
    | LeftBracket of position: Position
    | RightBracket of position: Position
    | Assignment of symbol: char * position: Position
    | Quote of position: Position
    | CommentIndicator of symbol: char * position: Position
    | Comment of text: string * position: Position
    | LineContinuation of position: Position
    | EscapedChar of char: char * position: Position
    | EscapedUnicodeChar of codepoint: int * position: Position
    | Text of text: string * position: Position
    | Whitespace of text: string * position: Position
with
    override this.ToString () =
        let escapeControlChars = String.replace "\r" "\\r" >> String.replace "\n" "\\n"

        match this with
        | LeftBracket _ -> "LeftBracket '['"
        | RightBracket _ -> "RightBracket ']'"
        | Assignment (symbol, _) -> $"Assignment '{symbol}'"
        | Quote _ -> "Quote '\"'"
        | CommentIndicator (symbol, _) -> $"CommentIndicator '{symbol}'"
        | Comment (text, _) -> $"Comment '{escapeControlChars text}'"
        | LineContinuation _ -> "LineContinuation '\\'"
        | EscapedChar (c, _) -> $"EscapedChar '\\{c}'"
        | EscapedUnicodeChar (codepoint, _) -> $"EscapedUnicodeChar '\\x%04x{codepoint}'"
        | Text (text, _) -> $"Text '{text}'"
        | Whitespace (text, _) -> $"Whitespace '{escapeControlChars text}'"

and Position =
    | PositionUndetermined
    | At of offset: int * line: int * column: int
with
    static member internal Start = At (0, 1, 1)

    static member inline private ``match`` fn p =
        match p with
        | At (offset, line, column) -> fn (offset, line, column)
        | PositionUndetermined -> failwith $"Invalid token {p}"

    static member inline toTuple p = p |> Position.``match`` id

    static member incrementLine p = p |> Position.``match`` (fun (offset, line, _) -> At (offset + 1, line + 1, 1))

    static member incrementColumn p = p |> Position.``match`` (fun (offset, line, column) -> At (offset + 1, line, column + 1))

    override this.ToString() = this |> Position.``match`` (fun (_, line, column) -> $"line {line}, column {column}")

module Token =
    let private escapeCharacters = [
        '\\', '\\'
        '0', '\u0000'
        'a', '\a'
        'b', '\b'
        'n', '\n'
        'r', '\r'
        't', '\t'
        '\'', '\''
        '"', '"'
        '=', '='
        ':', ':'
        ';', ';'
        '#', '#'
    ]

    let charToEscaped =
        escapeCharacters
        |> List.map (fun (escape, c) -> c, string escape)

    let inline private escapeCata fChooseValues map options text =
        let rec escape' escapeChars text =
            match escapeChars with
            | [] -> text
            | pair::rest ->
                let oldValue, newValue = fChooseValues pair
                let nextText = String.replace (string oldValue) (string newValue) text
                escape' rest nextText

        match options.escapeSequenceRule with
        | IgnoreEscapeSequences -> text
        | _ -> escape' map text

    let escape options text = escapeCata (fun (unescaped, escapeChar) -> (unescaped, "\\" + escapeChar)) charToEscaped options text 
    let unescape options = escapeCata (fun (escapeChar, unescaped) -> unescaped, escapeChar) escapeCharacters options

    let rec toText options token =
        match token with
            | LeftBracket _ -> "["
            | RightBracket _ -> "]"
            | Quote _ -> "\""
            | Assignment (symbol, _)
            | CommentIndicator (symbol, _) ->
                string symbol
            | Comment (text, _) 
            | Text (text, _)
            | Whitespace (text, _) ->
                text
            | LineContinuation _ -> "\\"
            | EscapedChar (c, _) ->
                match options.escapeSequenceRule with
                | IgnoreEscapeSequences -> c |> string
                | _ -> $"\\{c}"
            | EscapedUnicodeChar (codepoint, _) ->
                match options.escapeSequenceRule with
                | IgnoreEscapeSequences -> codepoint |> char |> string
                | _ -> $"\x%04x{codepoint}"

    let toTextToken token =
        match token with
        | LeftBracket position -> Text ("[", position)
        | RightBracket position -> Text ("]", position)
        | Assignment (c, position) -> Text (string c, position)

    let endsWith options substring token =
        token
        |> toText options
        |> String.endsWith substring

    let withPosition position token =
        match token with
        | LeftBracket _ -> LeftBracket position
        | RightBracket _ -> RightBracket position
        | Assignment (symbol, _) -> Assignment (symbol, position)
        | Quote _ -> Quote position
        | CommentIndicator (symbol, _) -> CommentIndicator (symbol, position)
        | Comment (text, _) -> Comment (text, position)
        | LineContinuation _ -> LineContinuation position
        | EscapedChar (c, _) -> EscapedChar (c, position)
        | EscapedUnicodeChar (codepoint, _) -> EscapedUnicodeChar (codepoint, position)
        | Text (text, _) -> Text (text, position)
        | Whitespace (text, _) -> Whitespace (text, position)

    let position token =
        match token with
        | LeftBracket position
        | RightBracket position
        | Assignment (_, position)
        | Quote position
        | CommentIndicator (_, position)
        | Comment (_, position)
        | LineContinuation position
        | EscapedChar (_, position)
        | EscapedUnicodeChar (_, position)
        | Text (_, position)
        | Whitespace (_, position) ->
            position
            //match position with
            //| At (offset, line, column) -> offset, line, column

    let endPosition token =
        let offset, line, column = token |> position |> Position.toTuple

        let endOffset, endLine, endColumn =
            match token with
            | LeftBracket _
            | RightBracket _
            | Assignment (_, _)
            | Quote _
            | CommentIndicator (_, _)
            | LineContinuation _ ->
                offset + 1, line, column + 1
            | EscapedChar (_, _) ->
                offset + 2, line, column + 2
            | EscapedUnicodeChar (_, _) ->
                offset + 6, line, column + 6
            | Comment (text, _)
            | Text (text, _)
            | Whitespace (text, _) ->
                let lineOffset =
                    text
                    |> Array.ofSeq
                    |> Array.filter ((=) '\n')
                    |> Array.length
                let columnOffset =
                    if lineOffset > 0 then
                        text.Length - (text.LastIndexOf('\n'))
                    else
                        column + text.Length
                (offset + text.Length, line + lineOffset, columnOffset)

        At (endOffset, endLine, endColumn)
