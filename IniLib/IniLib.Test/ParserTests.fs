module IniLib.Test.ParserTests

open IniLib
open Xunit

[<Fact>]
let ``Whitespace not included in key value`` () =
    let options = Options.defaultOptions.WithGlobalKeysRule(AllowGlobalKeys).WithNameValueDelimiterRule(ColonDelimiter).WithQuotationRule(UseQuotation)
    let text = "foo: bar\n\
                prince vegeta: \"nine thousand\"\n\
                \n"
    let config = Configuration.fromText options text
    let actual = Configuration.get "<global>" "foo" config
    let expected = "bar"
    Assert.Equal(expected, actual)

[<Fact>]
let ``Key with assignment token followed by end-of-file has an empty value`` () =
    let options = Options.defaultOptions
    let text = "[Section 1]\n\
                foo = "
    let config = Configuration.fromText options text
    let expected = ""
    let actual = Configuration.get "Section 1" "foo" config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Key with assignment token followed by newline has an empty value`` () =
    let options = Options.defaultOptions
    let text = "[Section 1]\n\
                foo = \n\
                [Section 2]\n\
                bar = baz"
    let config = Configuration.fromText options text
    let expected = ""
    let actual = Configuration.get "Section 1" "foo" config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Quoted key with assignment token followed by newline has an empty value`` () =
    let options = Options.defaultOptions.WithQuotationRule UseQuotation
    let text = "[Section 1]\n\
                \"foo\" = \n\
                [Section 2]\n\
                bar = baz"
    let config = Configuration.fromText options text
    let expected = ""
    let actual = Configuration.get "Section 1" "foo" config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Key with no assignment token followed by end-of-file has an empty value`` () =
    let options = Options.defaultOptions.WithNameValueDelimiterRule NoDelimiter
    let text = "[Section 1]\n\
                foo"
    let config = Configuration.fromText options text
    let expected = ""
    let actual = Configuration.get "Section 1" "foo" config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Key with no assignment token followed by newline has an empty value`` () =
    let options = Options.defaultOptions.WithNameValueDelimiterRule NoDelimiter
    let text = "[Section 1]\n\
                foo\n\
                bar 5"
    let config = Configuration.fromText options text
    let expected = ""
    let actual = Configuration.get "Section 1" "foo" config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Quoted Key with no assignment token followed by newline has an empty value`` () =
    let options =
        Options.defaultOptions
        |> Options.withNameValueDelimiterRule NoDelimiter
        |> Options.withQuotationRule UseQuotation
    let text = "[Section 1]\n\
                \"foo\"\n\
                bar 5"
    let config = Configuration.fromText options text
    let expected = ""
    let actual = Configuration.get "Section 1" "foo" config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Quoted key value followed by non-whitespace text returns an error`` () =
    let options = Options.defaultOptions.WithQuotationRule UseQuotation
    let text = "[Section 1]\n\
                foo = \"bar\" baz = quux"
    Assert.ThrowsAny (fun _ -> Configuration.fromText options text |> ignore) |> ignore

[<Fact>]
let ``A quote in the middle of a key value that already started without a quote returns an error`` () =
    let options = Options.defaultOptions.WithQuotationRule UseQuotation
    let text = "[Section 1]\n\
                foo = bar \"baz\""
    Assert.ThrowsAny (fun _ -> Configuration.fromText options text |> ignore) |> ignore

[<Fact>]
let ``Unclosed quoted key value at the end of a file returns an error`` () =
    let options = Options.defaultOptions.WithQuotationRule UseQuotation
    let text = "[Section 1]\n\
                foo = \"bar"
    Assert.ThrowsAny (fun _ -> Configuration.fromText options text |> ignore) |> ignore
