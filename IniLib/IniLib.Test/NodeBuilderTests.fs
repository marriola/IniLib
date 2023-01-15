module IniLib.Test.NodeBuilderTests

open IniLib
open Xunit

[<Fact>]
let ``sanitize wraps key name text with whitespace in replaceable quote token nodes with delimiter rule NoDelimiter and quotation rule UseQuotation`` () =
    let keyNode = NodeBuilder.key Options.defaultOptions "one word" "thundercougarfalconbird"
    let options =
        Options.defaultOptions
        |> Options.withQuotationRule UseQuotation
        |> Options.withNameValueDelimiterRule NoDelimiter
    let (KeyNode (_, _, (KeyNameNode _ as keyNameNode)::_)) = keyNode
    let (KeyNameNode (_, sanitizedKeyNameChildren)) = NodeBuilder.sanitize options keyNameNode

    match sanitizedKeyNameChildren with
    | ReplaceableTokenNode (Quote _) :: _ :: ReplaceableTokenNode (Quote _) :: _
    | _ :: ReplaceableTokenNode (Quote _) :: _ :: ReplaceableTokenNode (Quote _) :: _ ->
        ()
    | _ ->
        Assert.Fail $"Expected [ReplaceableTokenNode (Quote _); _; ReplaceableTokenNode (Quote _); ...], got %O{sanitizedKeyNameChildren}"

[<Fact>]
let ``sanitize does not escape key name with escape sequence rule UseEscapeSequences`` () =
    let keyNode = NodeBuilder.key Options.defaultOptions "one word" "thundercougarfalconbird"
    let options =
        Options.defaultOptions
        |> Options.withEscapeSequenceRule UseEscapeSequences
        |> Options.withNameValueDelimiterRule NoDelimiter
    let (KeyNode (_, _, (KeyNameNode _ as keyNameNode)::_)) = keyNode
    let (KeyNameNode (sanitizedKeyName, _)) = NodeBuilder.sanitize options keyNameNode
        
    Assert.Equal("one word", sanitizedKeyName)

[<Fact>]
let ``sanitize does not escape text of node twice with escape sequence rule UseEscapeSequences`` () =
    let keyNode = NodeBuilder.key Options.defaultOptions "one word" "thundercougarfalconbird"
    let options =
        Options.defaultOptions
        |> Options.withEscapeSequenceRule UseEscapeSequences
        |> Options.withNameValueDelimiterRule NoDelimiter
    let (KeyNode (_, _, (KeyNameNode _ as keyNameNode)::_)) = keyNode
    let sanitizedKeyNameNode1 = NodeBuilder.sanitize options keyNameNode
    let (KeyNameNode (_, sanitizedKeyNameChildren)) = NodeBuilder.sanitize options sanitizedKeyNameNode1

    let expectedText = "one\\ word"
    let actualText =
        match sanitizedKeyNameChildren with
        | ReplaceableTokenNode (Text (text, _, _)) :: _
        | _ :: ReplaceableTokenNode (Text (text, _, _)) :: _ ->
            text
        | _ ->
            failwith $"Expected [...; ReplaceableTextNode (Text _); ...], got %O{sanitizedKeyNameChildren}"

    Assert.Equal(expectedText, actualText)

[<Fact>]
let ``sanitize escapes text with escape characters with escape sequence rule UseEscapeSequences`` () =
    let keyNode = NodeBuilder.key Options.defaultOptions "one word" "thundercougarfalconbird"
    let options =
        Options.defaultOptions
        |> Options.withEscapeSequenceRule UseEscapeSequences
        |> Options.withNameValueDelimiterRule NoDelimiter
    let (KeyNode (_, _, (KeyNameNode _ as keyNameNode)::_)) = keyNode
    let (KeyNameNode (_, sanitizedKeyNameChildren)) = NodeBuilder.sanitize options keyNameNode

    let expectedText = "one\\ word"
    let actualText =
        match sanitizedKeyNameChildren with
        | ReplaceableTokenNode (Text (text, _, _)) :: _
        | _ :: ReplaceableTokenNode (Text (text, _, _)) :: _ ->
            text
        | _ ->
            failwith $"Expected [...; ReplaceableTextNode (Text _); ...], got %O{sanitizedKeyNameChildren}"

    Assert.Equal(expectedText, actualText)
