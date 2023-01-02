namespace IniLib

open IniLib.Utilities

type Token =
    | LeftBracket of line: int * column: int
    | RightBracket of line: int * column: int
    | Assignment of symbol: char * line: int * column: int
    | Quote of line: int * column: int
    | CommentIndicator of symbol: char * line: int * column: int
    | Comment of text: string * line: int * column: int
    | LineContinuation of line: int * column: int
    | EscapedChar of char: char * line: int * column: int
    | EscapedUnicodeChar of codepoint: int * line: int * column: int
    | Text of text: string * line: int * column: int
    | Whitespace of text: string * line: int * column: int
with
    override this.ToString () =
        let escapeControlChars = String.replace "\r" "\\r" >> String.replace "\n" "\\n"

        match this with
        | LeftBracket _ -> "LeftBracket '['"
        | RightBracket _ -> "RightBracket ']'"
        | Assignment (symbol, _, _) -> $"Assignment '{symbol}'"
        | Quote _ -> "Quote '\"'"
        | CommentIndicator (symbol, _, _) -> $"CommentIndicator '{symbol}'"
        | Comment (text, _, _) -> $"Comment '{escapeControlChars text}'"
        | LineContinuation _ -> "LineContinuation '\\'"
        | EscapedChar (c, _, _) -> $"EscapedChar '\\{c}'"
        | EscapedUnicodeChar (codepoint, _, _) -> $"EscapedUnicodeChar '\\x%04x{codepoint}'"
        | Text (text, _, _) -> $"Text '{text}'"
        | Whitespace (text, _, _) -> $"Whitespace '{escapeControlChars text}'"

and Position =
    | Position of line: int * column: int
with
    override this.ToString() =
        match this with
        | Position (line, column) -> sprintf "line %d, column %d" line column

    static member (+) (Position (line1, column1), (line2: int, column2: int)) =
        Position (line1 + line2, column1 + column2)

module Token =
    let private escapeCharacters = (*Map.ofList*) [
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
        //|> Map.toList
        |> List.map (fun (escape, c) -> c, string escape)
        //|> Map.ofList

    let (*inline*) private escapeCata fChooseValues map options text =
        let rec escape' escapeChars text =
            match escapeChars with
            | [] -> text
            | pair::rest ->
                let oldValue, newValue = fChooseValues pair
                let nextText = String.replace (string oldValue) (string newValue) text
                escape' rest nextText

        match options.escapeSequenceRule with
        | IgnoreEscapeSequences -> text
        | _ -> escape' map (*(Map.toList map)*) text

    let escape options text = escapeCata (fun (unescaped, escapeChar) -> (unescaped, "\\" + escapeChar)) charToEscaped options text 
    let unescape options = escapeCata (fun (escapeChar, unescaped) -> unescaped, escapeChar) escapeCharacters options

    let rec toText options token =
        match token with
            | LeftBracket _ -> "["
            | RightBracket _ -> "]"
            | Quote _ -> "\""
            | Assignment (symbol, _, _)
            | CommentIndicator (symbol, _, _) ->
                string symbol
            | Comment (text, _, _) 
            | Text (text, _, _)
            | Whitespace (text, _, _) ->
                text
            | LineContinuation _ -> "\\"
            | EscapedChar (c, _, _) ->
                match options.escapeSequenceRule with
                | IgnoreEscapeSequences -> c |> string
                | _ -> $"\\{c}"
            | EscapedUnicodeChar (codepoint, _, _) ->
                match options.escapeSequenceRule with
                | IgnoreEscapeSequences -> codepoint |> char |> string
                | _ -> $"\x%04x{codepoint}"

    let withPosition (line, column) token =
        match token with
        | LeftBracket _ -> LeftBracket (line, column)
        | RightBracket _ -> RightBracket (line, column)
        | Assignment (symbol, _, _) -> Assignment (symbol, line, column)
        | Quote _ -> Quote (line, column)
        | CommentIndicator (symbol, _, _) -> CommentIndicator (symbol, line, column)
        | Comment (text, _, _) -> Comment (text, line, column)
        | LineContinuation _ -> LineContinuation (line, column)
        | EscapedChar (c, _, _) -> EscapedChar (c, line, column)
        | EscapedUnicodeChar (codepoint, _, _) -> EscapedUnicodeChar (codepoint, line, column)
        | Text (text, _, _) -> Text (text, line, column)
        | Whitespace (text, _, _) -> Whitespace (text, line, column)

    let position token =
        match token with
        | LeftBracket (line, column)
        | RightBracket (line, column)
        | Assignment (_, line, column)
        | Quote (line, column)
        | CommentIndicator (_, line, column)
        | Comment (_, line, column)
        | LineContinuation (line, column)
        | EscapedChar (_, line, column)
        | EscapedUnicodeChar (_, line, column)
        | Text (_, line, column)
        | Whitespace (_, line, column) ->
            line, column

    let endPosition token =
        match token with
        | LeftBracket (line, column)
        | RightBracket (line, column)
        | Assignment (_, line, column)
        | Quote (line, column)
        | CommentIndicator (_, line, column)
        | LineContinuation (line, column) ->
            line, column + 1
        | EscapedChar (_, line, column) ->
            line, column + 2
        | EscapedUnicodeChar (_, line, column) ->
            line, column + 6
        | Comment (text, line, column)
        | Text (text, line, column)
        | Whitespace (text, line, column) ->
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
            (line + lineOffset, columnOffset)
