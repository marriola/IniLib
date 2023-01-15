module IniLib.Test.WriteTests

open IniLib
open Xunit

[<Fact>]
let ``Original whitespace is preserved when converting file to text after deleting a section`` () =
    let options = Options.defaultOptions
    let text = """
[ Section 1 ]
    foo=bar
    baz = quux ; Comment one
    a = 1      ; Comment two
    b = 2      ; Comment three
        
        
[Section 2]
z=1
y=   2
   x=3

"""
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.removeSection options "Section 1"
    let expected = """
[Section 2]
z=1
y=   2
   x=3

"""
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Original whitespace is preserved when converting file to text`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = """
[ Section 1 ]
foo=bar
baz = quux ; Comment one
a = 1      ; Comment two
b = 2      ; Comment three


[Section 2]
z=1
y=   2
x=3

"""
    let config = Configuration.fromText options text
    let textOut = Configuration.toText options config
    Assert.Equal(text, textOut)

[<Fact>]
let ``Original whitespace is preserved when converting file to text after deleting a key`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = """
[ Section 1 ]
foo=bar
baz = quux ; Comment one
a = 1      ; Comment two
b = 2      ; Comment three


[Section 2]
z=1
y=   2
x=3

"""
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.removeKey options "Section 1" "baz"
    let expected = """
[ Section 1 ]
foo=bar
a = 1      ; Comment two
b = 2      ; Comment three


[Section 2]
z=1
y=   2
x=3

"""
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Original whitespace is preserved when converting file to text after modifying a key`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = """
[ Section 1 ]
foo=bar
baz = quux ; Comment one
a = 1      ; Comment two
b = ?      ; Comment three


[Section 2]
z=1
y=   2
x=3

"""
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "b" "never gonna give you up"
    let expected = text.Replace("?", "never gonna give you up")
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)
