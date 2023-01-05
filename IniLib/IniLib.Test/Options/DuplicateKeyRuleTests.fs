namespace IniLib.Test.Options

module DuplicateKeyRuleTests =

    open IniLib
    open Xunit

    [<Fact>]
    let ``Duplicate key throws error when duplicate key rule is DisallowDuplicateKeys`` () =
        let options = Options.defaultOptions.WithDuplicateKeyRule DisallowDuplicateKeys
        let text = "[Section 1]\n\
                    foo = bar\n\
                    foo = quux"
        Assert.ThrowsAny(fun _ -> Configuration.fromText options text |> ignore) |> ignore

    [<Fact>]
    let ``Duplicate key adds value when duplicate key rule is AllowMultivalueKeys`` () =
        let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyAddsValue
        let text = "[Section 1]\n\
                    foo = bar\n\
                    foo = quux"
        let config = Configuration.fromText options text
        let fooValues = Configuration.getMultiValues "Section 1" "foo" config
        Assert.Contains("bar", fooValues)
        Assert.Contains("quux", fooValues)

    [<Fact>]
    let ``Multivalue keys are stored in the order parsed`` () =
        let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyAddsValue
        let text = "[Section 1]\n\
                    foo = 9\n\
                    foo = 0\n\
                    foo = 2\n\
                    foo = 1\n\
                    foo = 0"
        let config = Configuration.fromText options text
        let fooValues = Configuration.getMultiValues "Section 1" "foo" config
        let expected = ["9"; "0"; "2"; "1"; "0"]
        Assert.Equal<string>(expected, fooValues)

    [<Fact>]
    let ``Duplicate key replaces value when duplicate key rule is DuplicateReplacesKey`` () =
        let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyReplacesValue
        let text = "[Section 1]\n\
                    foo = bar\n\
                    foo = quux"
        let config = Configuration.fromText options text
        let fooValues = Configuration.getMultiValues "Section 1" "foo" config
        Assert.DoesNotContain("bar", fooValues)
        Assert.Contains("quux", fooValues)

    [<Fact>]
    let ``Adding second value to key stores both keys in map`` () =
        let section = "Section 1"
        let key = "test key"
        let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyAddsValue
        let config =
            Configuration.empty
            |> Configuration.add options section key "value 1"
            |> Configuration.add options section key "value 2"

        let values = Configuration.getMultiValues section key config
        Assert.Contains("value 1", values)
        Assert.Contains("value 2", values)

    [<Fact>]
    let ``Second value replaces first when multivalue keys are disallowed`` () =
        let section = "Section 1"
        let key = "test key"
        let options = Options.defaultOptions.WithDuplicateKeyRule DisallowDuplicateKeys
        let config =
            Configuration.empty
            |> Configuration.add options section key "value 1"
            |> Configuration.add options section key "value 2"

        let values = Configuration.getMultiValues section key config
        Assert.DoesNotContain("value 1", values)
        Assert.Contains("value 2", values)

    [<Fact>]
    let ``Calling get on a multivalue key returns the last value`` () =
        let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyAddsValue
        let config =
            Configuration.empty
            |> Configuration.add options "Section 1" "testing" "ABC"
            |> Configuration.add options "Section 1" "testing" "123"
            |> Configuration.add options "Section 1" "testing" "IOU"
        Assert.Equal("IOU", Configuration.get "Section 1" "testing" config)

    [<Fact>]
    let ``Multivalue key appears in text output as duplicate keys in order added`` () =
        let options =
            Options.defaultOptions
            |> Options.withDuplicateKeyRule DuplicateKeyAddsValue
            |> Options.withNewlineRule LfNewline

        let config =
            Configuration.empty
            |> Configuration.add options "Section 1" "up" "down"
            |> Configuration.add options "Section 1" "testing" "123"
            |> Configuration.add options "Section 1" "testing" "456"
            |> Configuration.add options "Section 1" "charm" "strange"

        let expected = "[Section 1]\n\
                        up = down\n\
                        testing = 123\n\
                        testing = 456\n\
                        charm = strange\n\
                        \n"

        Assert.Equal(expected, Configuration.toText options config)

    [<Fact>]
    let ``Deleting multivalue key removes key from the map`` () =
        let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyAddsValue
        let section, key = "foo", "bar"
        let config =
            Configuration.empty
            |> Configuration.add options section key "baz"
            |> Configuration.add options section key "quux"
            |> Configuration.removeKey options section key

        Assert.Null(Configuration.tryGet section key config)

    [<Fact>]
    let ``Deleting multivalue key removes all instances of the key from the text output`` () =
        let options =
            Options.defaultOptions
            |> Options.withDuplicateKeyRule DuplicateKeyAddsValue
            |> Options.withNewlineRule LfNewline
        let section, key = "foo", "bar"
        let config =
            Configuration.empty
            |> Configuration.add options section "up" "down"
            |> Configuration.add options section key "baz"
            |> Configuration.add options section key "quux"
            |> Configuration.add options section "beauty" "truth"
            |> Configuration.removeKey options section key

        let expected = "[foo]\n\
                        up = down\n\
                        beauty = truth\n\
                        \n"

        let actual = Configuration.toText options config
        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Renaming multivalue key renames all matching keys`` () =
        let options =
            Options.defaultOptions
            |> Options.withDuplicateKeyRule DuplicateKeyAddsValue
            |> Options.withNewlineRule LfNewline

        let text = "[Section 1]\n\
                    stooges = larry\n\
                    stooges = moe\n\
                    stooges = curly\n\
                    \n\
                    [Section 2]\n\
                    beauty = truth"

        let actual =
            text
            |> Configuration.fromText options
            |> Configuration.renameKey options "Section 1" "stooges" "performers"
            |> Configuration.toText options

        let expected = text.Replace("stooges", "performers")
        Assert.Equal(expected, actual)
