namespace IniLib.Test.Options

module GlobalKeysRuleTests =

    open IniLib
    open Xunit

    [<Fact>]
    let ``Adding a global key does not add a section heading`` () =
        let options =
            Options.defaultOptions
            |> Options.withGlobalKeysRule AllowGlobalKeys
            |> Options.withNewlineRule LfNewline
        let config =
            Configuration.empty
            |> Configuration.add options "<global>" "first key" "1"
        let text = Configuration.toText options config
        let expected = "first key = 1\n\n"
        Assert.Equal(expected, text)

    [<Fact>]
    let ``Global section is inserted before any other section`` () =
        let options =
            Options.defaultOptions
            |> Options.withGlobalKeysRule AllowGlobalKeys
            |> Options.withNewlineRule LfNewline
        let config =
            Configuration.empty
            |> Configuration.add options "Section 1" "foo" "bar"
            |> Configuration.add options "<global>" "first key" "1"
        let text = Configuration.toText options config
        let expected = "first key = 1\n\
                        \n\
                        [Section 1]\n\
                        foo = bar\n\
                        \n"
        Assert.Equal(expected, text)

    [<Fact>]
    let ``Global section is inserted after initial comments`` () =
        let options =
            Options.defaultOptions
            |> Options.withGlobalKeysRule AllowGlobalKeys
            |> Options.withNewlineRule LfNewline
        let text = "; Comment at beginning\n\
                    \n\
                    [Section 1]\n\
                    foo = bar\n\
                    \n"
        let config =
            text
            |> Configuration.fromText options
            |> Configuration.add options "<global>" "first key" "1"
        let expected = "; Comment at beginning\n\
                        \n\
                        first key = 1\n\
                        \n\
                        [Section 1]\n\
                        foo = bar\n\
                        \n"
        let actual = Configuration.toText options config
        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Parsing a global key when not enabled throws an error`` () =
        let options = Options.defaultOptions.WithGlobalKeysRule DisallowGlobalKeys
        let text = "this key = is global"
        Assert.ThrowsAny(fun _ -> Configuration.fromText options text |> ignore) |> ignore

    [<Fact>]
    let ``Global key is added to <global> section when allowed`` () =
        let options = Options.defaultOptions.WithGlobalKeysRule AllowGlobalKeys
        let text = "this key = is global"
        let config = Configuration.fromText options text
        Assert.Equal("is global", Configuration.get "<global>" "this key" config)

    [<Fact>]
    let ``Global keys are present when configuration is parsed and converted to text`` () =
        let options =
            Options.defaultOptions
            |> Options.withGlobalKeysRule AllowGlobalKeys
            |> Options.withNewlineRule LfNewline

        let text = "this key = is global\n\
                    so is = this one"

        let config =
            text
            |> Configuration.fromText options
            |> Configuration.add options "Section 1" "foo" "bar"

        let expected = "this key = is global\n\
                        so is = this one\n\
                        \n\
                        [Section 1]\n\
                        foo = bar"
        let actual = Configuration.toText options config
        Assert.Equal(expected, actual.Trim())
