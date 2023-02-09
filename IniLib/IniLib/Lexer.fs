module internal IniLib.Lexer

open System

let private HEX_DIGITS = Set.ofList ([ '0' .. '9' ] @ [ 'a' .. 'f'] @ [ 'A' .. 'F' ])

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

    let readLine startPosition text =
        let rec readLine' outputText position text =
            let nextPosition = Position.incrementColumn position
            match text with
            // Break on newline to force it to be its own token
            | '\r'::_
            | '\n'::_
            | [] ->
                let position = Position.incrementColumn startPosition
                text, nextPosition, Comment (new String(outputText |> List.rev |> Array.ofList), position)
            | c::rest ->
                readLine' (c :: outputText) nextPosition rest

        readLine' [] startPosition text

    let readNumbers startPosition text =
        let rec readNumbers' outputText position text =
            let nextPosition =
                match text with
                | '\n'::_ -> Position.incrementLine position
                | _ -> Position.incrementColumn position
            match text with
            | c::rest when List.length outputText < 4 && HEX_DIGITS.Contains(c) ->
                readNumbers' (c :: outputText) nextPosition rest
            | _ ->
                new String(outputText |> List.rev |> Array.ofList), nextPosition, text
        readNumbers' [] startPosition text

    let rec lex' output position text =
        let nextPosition =
            match text with
            | '\n'::_ -> Position.incrementLine position
            | _ -> Position.incrementColumn position

        match output, text with
        | _, [] -> output

        | Whitespace (ws, _)::_,
          c::rest
          when
            Char.IsWhiteSpace(c)
            && (List.isEmpty output
                || c = '\r'
                || c = '\n' && not (ws.EndsWith "\r")) ->
            let nextOutput = Whitespace (string c, position) :: output
            lex' nextOutput nextPosition rest

        | Whitespace (ws, lastPosition)::restOutput,
          c::rest
          when Char.IsWhiteSpace(c) && not (ws.EndsWith "\n") ->
            let whitespace = Whitespace (ws + string c, lastPosition)
            let nextOutput = whitespace :: restOutput
            lex' nextOutput nextPosition rest

        | _, c::rest
          when Char.IsWhiteSpace(c) ->
            let nextOutput = Whitespace (string c, position) :: output
            lex' nextOutput nextPosition rest

        | _, c::rest
          when Set.contains c assignmentIndicators ->
            let nextToken = Assignment (c, position)
            lex' (nextToken :: output) nextPosition rest

        | _, c::rest
          when Set.contains c commentIndicators ->
            let hashToken = CommentIndicator (c, position)
            let rest, nextPosition, commentToken = readLine position rest
            lex' (commentToken :: hashToken :: output) nextPosition rest

        | _, '"'::rest
          when options.quotationRule <> IgnoreQuotation ->
            let quoteToken = Quote position
            lex' (quoteToken :: output) nextPosition rest

        | _, '\\'::'\n'::rest
        | _, '\\'::'\r'::'\n'::rest
          when options.escapeSequenceRule = UseEscapeSequencesAndLineContinuation ->
            let lineContinuationToken = LineContinuation position
            lex' (lineContinuationToken :: output) nextPosition rest

        | _, '\\'::'x'::rest
          when options.escapeSequenceRule <> IgnoreEscapeSequences ->
            let number, nextPosition, rest = readNumbers position rest
            let codepoint = Int32.Parse(number, System.Globalization.NumberStyles.HexNumber)
            let escapeToken = EscapedUnicodeChar (codepoint, position)
            lex' (escapeToken :: output) nextPosition rest

        | _, '\\'::c::rest
          when options.escapeSequenceRule <> IgnoreEscapeSequences ->
            let escapeToken = EscapedChar (c, position)
            let nextPosition = Position.incrementColumn nextPosition
            lex' (escapeToken :: output) nextPosition rest

        | _, '['::rest ->
            let nextToken = LeftBracket position
            lex' (nextToken :: output) nextPosition rest

        | _, ']'::rest ->
            let nextToken = RightBracket position
            lex' (nextToken :: output) nextPosition rest

        | Text (t, lastPosition)::restOutput,
          c::rest ->
            let text = (t + string c)
            let nextToken = Text (text, lastPosition)
            let nextOutput = nextToken :: restOutput
            lex' nextOutput nextPosition rest

        | _, c::rest ->
            let nextToken = Text (string c, position)
            let nextOutput = nextToken :: output
            lex' nextOutput nextPosition rest

    text
    |> List.ofSeq
    |> lex' [] Position.Start
    |> List.rev
