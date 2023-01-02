module IniLib.NodeBuilder

open System.Text.RegularExpressions

let private RE_LEADING_OR_TRAILING_WHITESPACE = new Regex(["\\s+.*"; ".*?\\s+"; "\\s+.*?\\s+"] |> String.concat "|")

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
        Some (keyName name)
        assignmentToken
        Some (keyValue value)
    ]

    KeyNode (name, value, List.choose id children)
