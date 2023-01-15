module IniLib.Test.NodeTests

open IniLib
open Xunit

[<Fact>]
let ``Node text is unchanged for all comment nodes after rebuilding tree`` () =
    let options = Options.defaultOptions
    let text = "[Section 1]\n\
                # blah blah\n\
                foo = bar"
    let config = Configuration.fromText options text
    let [comment, _] = Configuration.getComments "Section 1" "foo" config
    Assert.Equal("blah blah", comment)
