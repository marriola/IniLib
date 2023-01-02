namespace IniLib.Test.Options

module NameValueDelimiterRuleTests =

    open IniLib
    open Xunit

    [<Fact>]
    let ``Parses equals delimiter`` () =
        let options = Options.defaultOptions.WithNameValueDelimiterRule EqualsDelimiter
        let text = "[Section 1]\n\
                    foo = bar"
        let config = Configuration.fromText options text
        Assert.Equal("bar", Configuration.get "Section 1" "foo" config)

    [<Fact>]
    let ``Writes equals delimiter with single space on each side`` () =
        let options =
            Options.defaultOptions
            |> Options.withNameValueDelimiterRule EqualsDelimiter
            |> Options.withNewlineRule LfNewline

        let expected = "[Section 1]\n\
                        foo = bar\n\
                        \n"
        let actual =
            Configuration.empty
            |> Configuration.add options "Section 1" "foo" "bar"
            |> Configuration.toText options

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Changes existing name-value delimiter to colon delimiter with space on right side only when converting configuration to text`` () =
        let text = "[Section 1]\n\
                    foo = bar"

        let expected = "[Section 1]\n\
                        foo: bar"

        let actual =
            text
            |> Configuration.fromText (Options.defaultOptions.WithNameValueDelimiterRule EqualsDelimiter)
            |> Configuration.toText (Options.defaultOptions.WithNameValueDelimiterRule ColonDelimiter)

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Changes existing name-value delimiter to equals delimiter with space on both sides when converting configuration to text`` () =
        let text = "[Section 1]\n\
                    foo: bar"

        let expected = "[Section 1]\n\
                        foo = bar"

        let actual =
            text
            |> Configuration.fromText (Options.defaultOptions.WithNameValueDelimiterRule ColonDelimiter)
            |> Configuration.toText (Options.defaultOptions.WithNameValueDelimiterRule EqualsDelimiter)

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Changes existing name-value delimiter to empty delimiter with single space in between when converting configuration to text`` () =
        let text = "[Section 1]\n\
                    foo = bar"

        let expected = "[Section 1]\n\
                        foo bar"

        let actual =
            text
            |> Configuration.fromText (Options.defaultOptions.WithNameValueDelimiterRule EqualsDelimiter)
            |> Configuration.toText (Options.defaultOptions.WithNameValueDelimiterRule NoDelimiter)

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Parses colon delimiter`` () =
        let options = Options.defaultOptions.WithNameValueDelimiterRule ColonDelimiter
        let text = "[Section 1]\n\
                    foo: bar"
        let config = Configuration.fromText options text
        Assert.Equal("bar", Configuration.get "Section 1" "foo" config)

    [<Fact>]
    let ``Writes colon delimiter with whitespace on right side only`` () =
        let options =
            Options.defaultOptions
            |> Options.withNameValueDelimiterRule ColonDelimiter
            |> Options.withNewlineRule LfNewline

        let expected = "[Section 1]\n\
                        foo: bar\n\
                        \n"
        let actual =
            Configuration.empty
            |> Configuration.add options "Section 1" "foo" "bar"
            |> Configuration.toText options

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Parses empty delimiter`` () =
        let options = Options.defaultOptions.WithNameValueDelimiterRule NoDelimiter
        let text = "[Section 1]\n\
                    foo bar"
        let config = Configuration.fromText options text
        Assert.Equal("bar", Configuration.get "Section 1" "foo" config)

    [<Fact>]
    let ``Writes empty delimiter with single whitespace in between key and value`` () =
        let options =
            Options.defaultOptions
            |> Options.withNameValueDelimiterRule NoDelimiter
            |> Options.withNewlineRule LfNewline

        let expected = "[Section 1]\n\
                        foo bar\n\
                        \n"
        let actual =
            Configuration.empty
            |> Configuration.add options "Section 1" "foo" "bar"
            |> Configuration.toText options

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Allows custom spacing around delimiter`` () =
        let options =
            Options.defaultOptions
            |> Options.withNameValueDelimiterRule EqualsDelimiter
            |> Options.withNameValueDelimiterSpacingRule NoSpacing
            |> Options.withNewlineRule LfNewline

        let expected = "[Section 1]\n\
                        foo=bar\n\
                        \n"
        let actual =
            Configuration.empty
            |> Configuration.add options "Section 1" "foo" "bar"
            |> Configuration.toText options

        Assert.Equal(expected, actual)
