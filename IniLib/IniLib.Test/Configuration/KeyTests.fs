module IniLib.Test.Configuration.KeyTests

open IniLib
open Xunit

[<Fact>]
let ``Add key to existing section`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar\n\
                \n"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "baz" "quux"

    let expected = "[Section 1]\n\
                    foo = bar\n\
                    baz = quux\n\
                    \n"

    Assert.Equal(expected, Configuration.toText options config)

[<Fact>]
let ``Adding key to existing section copies indentation of last key`` () =
    let options = Options.defaultOptions
    let text = """
[Section 1]
foo = bar"""
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "baz" "quux"

    let expected = """
[Section 1]
foo = bar
baz = quux
"""

    Assert.Equal(expected, Configuration.toText options config)

[<Fact>]
let ``Adding key to section that does not end in newline inserts a newline after the last key`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "baz" "quux"

    let expected = "[Section 1]\n\
                    foo = bar\n\
                    baz = quux\n"

    Assert.Equal(expected, Configuration.toText options config)

[<Fact>]
let ``Add a key to an empty section`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "foo" "bar"
    let expected = "[Section 1]\n\
                    foo = bar\n"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a key to an empty section with a newline-less section heading`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "foo" "bar"
    let expected = "[Section 1]\n\
                    foo = bar\n"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Replace single key value`` () =
    let section, key, value = "foo", "bar", "baz"
    let config = Configuration.add Options.defaultOptions section key value Configuration.empty
    let fetchedValue = Configuration.get section key config
    Assert.Equal(value, fetchedValue)

    let value = "quux"
    let config = Configuration.add Options.defaultOptions section key value config
    let fetchedValue = Configuration.get section key config
    Assert.Equal(value, fetchedValue)

[<Fact>]
let ``Replacing key value preserves existing whitespace`` () =
    let options = Options.defaultOptions
    let text = "[Section 1]\n\
                foo = bar\t"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "foo" "bear arms"
    let actual = Configuration.toText options config
    let expected = "[Section 1]\n\
                    foo = bear arms\t"
    Assert.Equal(expected, actual)

[<Fact>]
let ``Replacing key value preserves comment on same line`` () =
    let options = Options.defaultOptions
    let text = "[Section 1]\n\
                foo = bar\t; Comment"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "foo" "bear arms"
    let keyNode = Configuration.getNode "Section 1" "foo" config
    Assert.Equal("foo = bear arms\t; Comment", Node.toText options keyNode)

[<Fact>]
let ``Deleting a key also removes leading and trailing whitespace on the same line`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = """
[Section 1]
foo = foo
   bar = bar   
quux = quux"""
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.removeKey options "Section 1" "bar"
    let expected = """
[Section 1]
foo = foo
quux = quux"""
    Assert.Equal(expected, Configuration.toText options config)

[<Fact>]
let ``Deleting key with comment on same line also removes the comment`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                aaa = 1\n\
                foo = bar\t; Comment\n\
                bbb = 2"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.removeKey options "Section 1" "foo"
    let expected = "[Section 1]\n\
                    aaa = 1\n\
                    bbb = 2"
    Assert.Equal(expected, Configuration.toText options config)

[<Fact>]
let ``Deleting a key removes it from the map`` () =
    let section, key, value = "foo", "bar", "baz"
    let config =
        Configuration.empty
        |> Configuration.add Options.defaultOptions section key value
        |> Configuration.removeKey Options.defaultOptions section key

    Assert.Null(Configuration.tryGet section key config)

[<Fact>]
let ``Adding section with single key adds key to map`` () =
    let section, key, value = "foo", "bar", "baz"
    let config = Configuration.add Options.defaultOptions section key value Configuration.empty
    let fetchedValue = Configuration.get section key config

    Assert.Equal(fetchedValue, value)

[<Fact>]
let ``Rename key`` () =
    let options = Options.defaultOptions
    let config =
        Configuration.empty
        |> Configuration.add options "Section 1" "beauty" "42"
        |> Configuration.renameKey options "Section 1" "beauty" "truth"
    let expectedValues = ["42"]
    let actualValues = Configuration.getMultiValues "Section 1" "truth" config
    Assert.Equivalent(expectedValues, actualValues)
