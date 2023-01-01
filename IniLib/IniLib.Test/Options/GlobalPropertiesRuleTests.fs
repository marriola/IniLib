namespace IniLib.Test.Options

module GlobalPropertiesRuleTests =

    open IniLib
    open Xunit

    [<Fact>]
    let ``Parsing a global property when not enabled throws an error`` () =
        let options = Options.defaultOptions.WithGlobalPropertiesRule DisallowGlobalProperties
        let text = "this property = is global"
        Assert.ThrowsAny(fun _ -> Configuration.fromText options text |> ignore) |> ignore

    [<Fact>]
    let ``Global property is added to <global> section when allowed`` () =
        let options = Options.defaultOptions.WithGlobalPropertiesRule AllowGlobalProperties
        let text = "this property = is global"
        let config = Configuration.fromText options text
        Assert.Equal("is global", Configuration.get "<global>" "this property" config)

    [<Fact>]
    let ``Global properties are present when configuration is parsed and converted to text`` () =
        let options =
            Options.defaultOptions
            |> Options.withGlobalPropertiesRule AllowGlobalProperties
            |> Options.withNewlineRule LfNewline

        let text = "this property = is global\n\
                    so is = this one"

        let config =
            text
            |> Configuration.fromText options
            |> Configuration.add options "Section 1" "foo" "bar"

        let expected = "this property = is global\n\
                        so is = this one\n\
                        [Section 1]\n\
                        foo = bar"
        let actual = Configuration.toText options config
        Assert.Equal(expected, actual.Trim())
