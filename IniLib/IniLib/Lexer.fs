module internal IniLib.Lexer

open System

let lex options text =
    let assignmentIndicators =
        match options.nameValueDelimiterRule with
        | NoDelimiter -> Set.empty
        | EqualsDelimiter -> set "="
        | ColonDelimiter -> set ":"
        | EqualsOrColonDelimiter -> set "=:"

    let commentIndicators =
        match options.commentRule with
        | SemicolonComments -> set ";"
        | HashComments -> set "#"
        | HashAndSemicolonComments -> set "#;"

    let readLine firstLine firstColumn text =
        let rec readLine' outputText line column last text =
            let nextLine, nextColumn =
                match text with
                | '\n'::_ ->
                    line + 1, 1
                | _ ->
                    line, column + 1
            match last, text with
            | Some '\n', _
            | _, [] ->
                text, line, column, Comment (new String(outputText |> List.rev |> Array.ofList), firstLine, firstColumn + 1)
            | _, c::rest ->
                readLine' (c :: outputText) nextLine nextColumn (Some c) rest

        readLine' [] firstLine firstColumn None text

    let readNumbers firstLine firstColumn text =
        let hexDigits = Set.ofList ([ '0' .. '9' ] @ [ 'a' .. 'f'] @ [ 'A' .. 'F' ])
        let rec readNumbers' outputText line column text =
            let nextLine, nextColumn =
                match text with
                | '\n'::_ ->
                    line + 1, 1
                | _ ->
                    line, column + 1
            match text with
            | c::rest when List.length outputText < 4 && hexDigits.Contains(c) ->
                readNumbers' (c :: outputText) nextLine nextColumn rest
            | _ ->
                new String(outputText |> List.rev |> Array.ofList), nextLine, nextColumn, text
        readNumbers' [] firstLine firstColumn text

    let rec lex' output line column text =
        let nextLine, nextColumn =
            match text with
            | '\n'::_ ->
                line + 1, 1
            | _ ->
                line, column + 1

        match output, text with
        | _, [] -> output

        | Whitespace (ws, line, column)::restOutput, c::rest when Char.IsWhiteSpace(c) && not (ws.EndsWith("\n")) ->
            let whitespace = Whitespace (ws + string c, line, column)
            let nextOutput = whitespace :: restOutput
            lex' nextOutput nextLine nextColumn rest

        | _, c::rest when Char.IsWhiteSpace(c) ->
            let nextOutput = Whitespace (string c, line, column) :: output
            lex' nextOutput nextLine nextColumn rest

        | _, c::rest when Set.contains c assignmentIndicators ->
            let nextToken = Assignment (c, line, column)
            lex' (nextToken :: output) nextLine nextColumn rest

        | _, c::rest when Set.contains c commentIndicators ->
            let hashToken = CommentIndicator (c, line, column)
            let rest, nextLine, nextColumn, commentToken = readLine line column rest
            lex' (commentToken :: hashToken :: output) nextLine nextColumn rest

        | _, '"'::rest when options.quotationRule <> IgnoreQuotation ->
            let quoteToken = Quote (line, column)
            lex' (quoteToken :: output) nextLine nextColumn rest

        | _, '\\'::'\n'::rest
        | _, '\\'::'\r'::'\n'::rest when options.escapeSequenceRule = UseEscapeSequencesAndLineContinuation ->
            let lineContinuationToken = LineContinuation (line, column)
            lex' (lineContinuationToken :: output) nextLine nextColumn rest

        | _, '\\'::'x'::rest when options.escapeSequenceRule <> IgnoreEscapeSequences ->
            let number, nextLine, nextColumn, rest = readNumbers line column rest
            let escapeToken = EscapedUnicodeChar (Int32.Parse(number, System.Globalization.NumberStyles.HexNumber), line, column)
            let nextLine, nextColumn = Token.endPosition escapeToken
            lex' (escapeToken :: output) nextLine nextColumn rest

        | _, '\\'::c::rest when options.escapeSequenceRule <> IgnoreEscapeSequences ->
            let escapeToken = EscapedChar (c, line, column)
            lex' (escapeToken :: output) nextLine nextColumn rest

        | _, '['::rest ->
            let nextToken = LeftBracket (line, column)
            lex' (nextToken :: output) nextLine nextColumn rest

        | _, ']'::rest ->
            let nextToken = RightBracket (line, column)
            lex' (nextToken :: output) nextLine nextColumn rest

        | Text (t, line, column)::restOutput, c::rest ->
            let text = (t + string c)
            let nextToken = Text (text, line, column)
            let nextOutput = nextToken :: restOutput
            lex' nextOutput nextLine nextColumn rest

        | _, c::rest ->
            let nextToken = Text (string c, line, column)
            let nextOutput = nextToken :: output
            lex' nextOutput nextLine nextColumn rest

    text
    |> List.ofSeq
    |> lex' [] 1 1
    |> List.rev
