namespace IniLib.Test.Options

module GlobalKeysRuleTests =

    open IniLib
    open Xunit

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
                        [Section 1]\n\
                        foo = bar"
        let actual = Configuration.toText options config
        Assert.Equal(expected, actual.Trim())
