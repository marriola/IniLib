﻿namespace IniLib.Test.Options

module EscapeSequenceRuleTests =

    open IniLib
    open Xunit

    let validEscapeCharacters = [
        '0', char 0
        'a', char 7
        'b', char 8
        'f', char 12
        'n', char 10
        'r', char 13
        't', char 9
        'v', char 11
        '\\', '\\'
        '\'', '\''
        '"', '"'
        '#', '#'
        ':', ':'
        ';', ';'
        ' ', ' '
        's', ' '
    ]

    [<Fact>]
    let ``Escape sequences are correctly parsed and stored`` () =
        let options = Options.defaultOptions.WithEscapeSequenceRule UseEscapeSequences

        for (escapeCode, character) in validEscapeCharacters do
            let text = $"[Section 1]\n\
                         test = a\\{escapeCode}b"
            let expectedValue = $"a{character}b"
            let config = Configuration.fromText options text
            let actualValue = Configuration.get "Section 1" "test" config
            Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Unicode escape sequence is correctly parsed and stored`` () =
        let options = Options.defaultOptions.WithEscapeSequenceRule UseEscapeSequences
        let text = "[Section 1]\n\
                    foo = b\\x00e6r"
        let config = Configuration.fromText options text
        let expectedValue = "bær"
        let actualValue = Configuration.get "Section 1" "foo" config
        Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Line continuation in a key value inserts a newline`` () =
        let options =
            Options.defaultOptions
            |> Options.withEscapeSequenceRule UseEscapeSequencesAndLineContinuation
            |> Options.withNewlineRule LfNewline
        let text = "[Section 1]\n\
                    good night moon = good night cow jumping\\\n\
                    over the moon"
        let config = Configuration.fromText options text
        let expectedValue = "good night cow jumping\nover the moon"
        let actualValue = Configuration.get "Section 1" "good night moon" config
        Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Escape sequences are parsed literally when not enabled`` () =
        let options = Options.defaultOptions.WithEscapeSequenceRule IgnoreEscapeSequences
        let text = "[Section 1]\n\
                    foo = b\\x00e6r\n\
                    baz = \\\"quux\\\""
        let config = Configuration.fromText options text
        Assert.Equal("b\\x00e6r", Configuration.get "Section 1" "foo" config)
        Assert.Equal("\\\"quux\\\"", Configuration.get "Section 1" "baz" config)

    [<Fact>]
    let ``Key name may contain escape sequences`` () =
        let options =
            Options.defaultOptions
            |> Options.withNameValueDelimiterRule NoDelimiter
            |> Options.withEscapeSequenceRule UseEscapeSequences
        let text = "[Section 1]\n\
                    \"test\ k\\x0113y\" hello world"
        let expectedValue = "hello world"
        let config = Configuration.fromText options text
        let actualValue = Configuration.get "Section 1" "\"test kēy\"" config
        Assert.Equal(expectedValue, actualValue)

    [<Fact>]
    let ``Adding key name with whitespace in it causes whitespace to be escaped`` () =
        let options =
            Options.defaultOptions
            |> Options.withEscapeSequenceRule UseEscapeSequences
            |> Options.withNameValueDelimiterRule NoDelimiter
            |> Options.withNewlineRule LfNewline
        let config =
            Configuration.empty
            |> Configuration.add options "Section 1" "bar mitzvah" "9001"
        let text = Configuration.toText options config
        let expected = "[Section 1]\n\
                        bar\\ mitzvah 9001\n\
                        \n"
        Assert.Equal(expected, text)

    [<Fact>]
    let ``Changing delimiter to NoDelimiter after reading configuration escapes pre-existing key names with whitespace`` () =
        let options = Options.defaultOptions.WithNewlineRule LfNewline
        let text = "[Section 1]\n\
                    foo bar = 1\n\
                    baz = 2\n\
                    quux = 3\n\
                    \n"
        let config = Configuration.fromText options text
        let writeOptions = options.WithNameValueDelimiterRule(NoDelimiter).WithEscapeSequenceRule(UseEscapeSequences)
        let textWithNoDelimiter = Configuration.toText writeOptions config
        let expected = "[Section 1]\n\
                        foo\\ bar 1\n\
                        baz 2\n\
                        quux 3\n\
                        \n"
        Assert.Equal(expected, textWithNoDelimiter)
