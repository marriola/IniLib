module IniLib.Test.Configuration.SectionTests

open IniLib
open Xunit

[<Fact>]
let ``Rename section`` () =
    let options = Options.defaultOptions.WithDuplicateSectionRule AllowDuplicateSections
    let text = """
[Section 1]
foo = bar

[Section 2]
beauty = truth

[Section 1]
bar = baz"""

    let expected = text.Replace("Section 2", "90210")
        
    let actual =
        text
        |> Configuration.fromText options
        |> Configuration.renameSection options "Section 2" "90210"
        |> Configuration.toText options

    Assert.Equal(expected, actual)

[<Fact>]
let ``Adding a section after a section with no trailing newline inserts two newlines`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 2" "baz" "quux"
    let expected = "[Section 1]\n\
                    foo = bar\n\
                    \n\
                    [Section 2]\n\
                    baz = quux\n\
                    \n"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)
