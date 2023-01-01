namespace IniLib.Test.Options

module DuplicateSectionRuleTests =

    open Xunit
    open IniLib

    [<Fact>]
    let ``Throws an error on duplicate section when disallowed`` () =
        let options = Options.defaultOptions.WithDuplicateSectionRule DisallowDuplicateSections
        let text = "[Section 1]\n\
                    foo = 1\n\
                    [Section 1]\n\
                    bar = 2"
        Assert.ThrowsAny(fun _ -> Configuration.fromText options text |> ignore) |> ignore

    [<Fact>]
    let ``Combines keys from duplicate sections when allowed`` () =
        let options = Options.defaultOptions.WithDuplicateSectionRule AllowDuplicateSections
        let text = "[Section 1]\n\
                    foo = bar\n\
                    [Section 2]\n\
                    beauty = truth\n\
                    [Section 1]\n\
                    baz = quux"
        let config = Configuration.fromText options text
        Assert.Equal("bar", Configuration.get "Section 1" "foo" config)
        Assert.Equal("quux", Configuration.get "Section 1" "baz" config)

    [<Fact>]
    let ``Structure is preserved after changing key value in first duplicate section`` () =
        let options =
            Options.defaultOptions
            |> Options.withDuplicateSectionRule AllowDuplicateSections
            |> Options.withNewlineRule LfNewline

        let text = "[Section 1]\n\
                    foo = bar\n\
                    [Section 2]\n\
                    beauty = truth\n\
                    [Section 1]\n\
                    baz = quux"
        let config =
            text
            |> Configuration.fromText options
            |> Configuration.add options "Section 1" "foo" "bear arms"
        let expected = "[Section 1]\n\
                        foo = bear arms\n\
                        [Section 2]\n\
                        beauty = truth\n\
                        [Section 1]\n\
                        baz = quux"
        let actual = Configuration.toText options config
        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Structure is preserved after changing key value in second duplicate section`` () =
        let options =
            Options.defaultOptions
            |> Options.withDuplicateSectionRule AllowDuplicateSections
            |> Options.withNewlineRule LfNewline

        let text = "[Section 1]\n\
                    foo = bar\n\
                    [Section 2]\n\
                    beauty = truth\n\
                    [Section 1]\n\
                    baz = quux"

        let config =
            text
            |> Configuration.fromText options
            |> Configuration.add options "Section 1" "baz" "crux"

        let expected = "[Section 1]\n\
                        foo = bar\n\
                        [Section 2]\n\
                        beauty = truth\n\
                        [Section 1]\n\
                        baz = crux"
        let actual = Configuration.toText options config
        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Duplicate key in duplicate section replaces original value in map`` () =
        let options =
            Options.defaultOptions
            |> Options.withDuplicateSectionRule AllowDuplicateSections
            |> Options.withDuplicateKeyRule DuplicateKeyReplacesValue
        
        let text = "[Section 1]\n\
                    foo = bar\n\
                    [Section 2]\n\
                    beauty = truth\n\
                    [Section 1]\n\
                    foo = quux"

        let config = Configuration.fromText options text
        Assert.Equal<string>([ "quux" ], Configuration.getMultiValues "Section 1" "foo" config)

    [<Fact>]
    let ``Structure is preserved when duplicate key in duplicate section replaces a previous value`` () =
        let options =
            Options.defaultOptions
            |> Options.withDuplicateSectionRule AllowDuplicateSections
            |> Options.withDuplicateKeyRule DuplicateKeyReplacesValue
        
        let text = "[Section 1]\n\
                    foo = bar\n\
                    [Section 2]\n\
                    beauty = truth\n\
                    [Section 1]\n\
                    foo = quux"

        let config = Configuration.fromText options text
        let reconstitutedText = Configuration.toText options config
        Assert.Equal(text, reconstitutedText)

    [<Fact>]
    let ``Renaming multivalue key renames all matching keys across all sections`` () =
        let options =
            Options.defaultOptions
            |> Options.withDuplicateKeyRule DuplicateKeyAddsValue
            |> Options.withDuplicateSectionRule AllowDuplicateSections
            |> Options.withNewlineRule LfNewline

        let text = "[Section 1]\n\
                    stooges = larry\n\
                    stooges = moe\n\
                    stooges = curly\n\
                    \n\
                    [Section 2]\n\
                    beauty = truth\n\
                    \n\
                    [Section 1]\n\
                    stooges = shemp"

        let actual =
            text
            |> Configuration.fromText options
            |> Configuration.renameKey options "Section 1" "stooges" "performers"
            |> Configuration.toText options

        let expected = text.Replace("stooges", "performers")
        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Merge original section into duplicate`` () =
        let options = Options.defaultOptions.WithDuplicateSectionRule MergeOriginalSectionIntoDuplicate
        let text = """
[Section 1]
foo = bar

[Section 2]
beauty = truth

[Section 1]
bar = baz
"""

        let expected = """
[Section 2]
beauty = truth

[Section 1]
foo = bar
bar = baz
"""

        let actual =
            text
            |> Configuration.fromText options
            |> Configuration.toText options

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``Merge duplicate section into original`` () =
        let options = Options.defaultOptions.WithDuplicateSectionRule MergeDuplicateSectionIntoOriginal
        let text = """
[Section 1]
foo = bar

[Section 2]
beauty = truth

[Section 1]
bar = baz
"""

        let expected = """
[Section 1]
foo = bar
bar = baz

[Section 2]
beauty = truth

"""

        let actual =
            text
            |> Configuration.fromText options
            |> Configuration.toText options

        Assert.Equal(expected, actual)
