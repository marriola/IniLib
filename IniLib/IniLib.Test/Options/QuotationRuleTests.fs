namespace IniLib.Test.Options

module QuotationRuleTests =

    open IniLib
    open Xunit

    [<Fact>]
    let ``Quotation marks are parsed literally when not enabled`` () =
        let options = Options.defaultOptions
        let text = "[Section 1]\n\
                    foo = \"bar\""
        let config = Configuration.fromText options text
        let expectedValue = "\"bar\""
        let actualValue = Configuration.get "Section 1" "foo" config
        Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Quotation marks are not included in key value when enabled`` () =
        let options = Options.defaultOptions.WithQuotationRule UseQuotation
        let text = "[Section 1]\n\
                    foo = \"bar\""
        let config = Configuration.fromText options text
        let expectedValue = "bar"
        let actualValue = Configuration.get "Section 1" "foo" config
        Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Quotation marks preserve leading and trailing whitespace in key value when enabled`` () =
        let options = Options.defaultOptions.WithQuotationRule UseQuotation
        let text = "[Section 1]\n\
                    foo = \"   bar \""
        let config = Configuration.fromText options text
        let expectedValue = "   bar "
        let actualValue = Configuration.get "Section 1" "foo" config
        Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Escape sequences are parsed and stored correctly when quoted`` () =
        let options =
            Options.defaultOptions
            |> Options.withQuotationRule UseQuotation
            |> Options.withEscapeSequenceRule UseEscapeSequences
        for (escapeCode, character) in EscapeSequenceRuleTests.validEscapeCharacters do 
            let text = $"[Section 1]\n\
                         test = \\\"a\\{escapeCode}b\\\""
            let expectedValue = $"a{character}b"
            let config = Configuration.fromText options text
            let actualValue = Configuration.get "Section 1" "test" config
            Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Leading and trailing whitespace around a newly added key value are preserved in quotes`` () =
        let options =
            Options.defaultOptions
            |> Options.withQuotationRule UseQuotation
            |> Options.withNewlineRule LfNewline

        let config =
            Configuration.empty
            |> Configuration.add options "Section 1" "foo" "   bar   "

        let expected = "[Section 1]\n\
                        foo = \"   bar   \"\n\
                        \n"

        let actual = Configuration.toText options config
        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Always writes values in quotes when quotation rule is AlwaysUseQuotation`` () =
        let options =
            Options.defaultOptions
            |> Options.withQuotationRule AlwaysUseQuotation
            |> Options.withNewlineRule LfNewline
        let config =
            Configuration.empty
            |> Configuration.add options "Section 1" "foo" "bar"
        let expected = "[Section 1]\n\
                        foo = \"bar\"\n\
                        \n"
        let actual = Configuration.toText options config
        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Key name may be quoted`` () =
        let options =
            Options.defaultOptions
            |> Options.withQuotationRule UseQuotation
            |> Options.withNameValueDelimiterRule NoDelimiter
        let text = "[Section 1]\n\
                    \"bar mitzvah\" 9001"
        let config = Configuration.fromText options text
        let expectedValue = "9001"
        let actualValue = Configuration.get "Section 1" "bar mitzvah" config
        Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Adding key name with whitespace in it causes key name to be quoted`` () =
        let options =
            Options.defaultOptions
            |> Options.withQuotationRule UseQuotation
            |> Options.withNameValueDelimiterRule NoDelimiter
            |> Options.withNewlineRule LfNewline
        let config =
            Configuration.empty
            |> Configuration.add options "Section 1" "bar mitzvah" "9001"
        let text = Configuration.toText options config
        let expected = "[Section 1]\n\
                        \"bar mitzvah\" 9001\n\
                        \n"
        Assert.Equal(expected, text)
