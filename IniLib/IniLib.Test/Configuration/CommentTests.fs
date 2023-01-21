module IniLib.Test.Configuration.CommentTests

open IniLib
open Xunit

[<Fact>]
let ``tryGetKeyComments returns all comments immediately preceding, and on the same line as a key`` () =
    let options = Options.defaultOptions
    let text = "[Section 1]\n\
                # Comment 1\n\
                # Comment 2\n\
                foo = bar # Same line comment"
    let comments =
        text
        |> Configuration.fromText options
        |> Configuration.tryGetKeyComments "Section 1" "foo"
        |> Option.get
        |> List.map fst

    Assert.Equal<string>([ "Comment 1"; "Comment 2"; "Same line comment" ], comments)

[<Fact>]
let ``tryGetKeyComments does not return preceding comments separated by an empty line`` () =
    let options = Options.defaultOptions
    let text = "[Section 1]\n\
                # Comment 1\n\
                # Comment 2\n\
                \n\
                foo = bar # Same line comment"
    let comments =
        text
        |> Configuration.fromText options
        |> Configuration.tryGetKeyComments "Section 1" "foo"
        |> Option.get
        |> List.map fst

    Assert.Equal<string>([ "Same line comment" ], comments)

[<Fact>]
let ``tryGetKeyComments returns all comments on the same line as a multivalue key`` () =
    let options = Options.defaultOptions.WithDuplicateKeyRule DuplicateKeyAddsValue
    let text = "[Section 1]\n\
                # Comment 1\n\
                # Comment 2\n\
                foo = 1 # Same line comment 1\n\
                foo = 2 # Same line comment 2\n\
                # Comment 3\n\
                foo = 3 # Same line comment 3\n\
                # Comment 4\n\
                foo = 4 # Same line comment 4\n\
                \n"
    let comments =
        text
        |> Configuration.fromText options
        |> Configuration.tryGetKeyComments "Section 1" "foo"
        |> Option.get
        |> List.map fst

    let expected =
        [ "Comment 1"
          "Comment 2"
          "Same line comment 1"
          "Same line comment 2"
          "Comment 3"
          "Same line comment 3"
          "Comment 4"
          "Same line comment 4" ]

    Assert.Equal<string>(expected, comments)

[<Fact>]
let ``tryGetKeyComments returns comments for keys across duplicate sections`` () =
    let options =
        Options.defaultOptions
        |> Options.withDuplicateKeyRule DuplicateKeyAddsValue
        |> Options.withDuplicateSectionRule AllowDuplicateSections
    let text = "[Section 1]\n\
                # Comment 1\n\
                foo = bar # Same line comment 1\n\
                \n\
                [Section 2]\n\
                # ignore me\n\
                beauty = truth # and me\n\
                \n\
                [Section 1]\n\
                # Comment 2\n\
                foo = baz # Same line comment 2"
    let comments =
        text
        |> Configuration.fromText options
        |> Configuration.tryGetKeyComments "Section 1" "foo" 
        |> Option.get
        |> List.map fst

    Assert.Equal<string>([ "Comment 1"; "Same line comment 1"; "Comment 2"; "Same line comment 2" ], comments)

[<Fact>]
let ``changeComment changes the text of an existing comment`` () =
    let options = Options.defaultOptions
    let text = "[Section 1]\n\
                son goku = 9001 # blah"
    let config = Configuration.fromText options text
    let (Some [comment]) = Configuration.tryGetKeyComments "Section 1" "son goku" config
    let newConfig = Configuration.changeComment options "aka kakkarot" comment config
    let (Some [newCommentText, _]) = Configuration.tryGetKeyComments "Section 1" "son goku" newConfig
    Assert.Equal("aka kakkarot", newCommentText)

[<Fact>]
let ``Add comment on the same line as a key without a trailing newline`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar"
    let config = Configuration.fromText options text
    let key = Configuration.getNode "Section 1" "foo" config
    let config = Configuration.addComment OnSameLine options key "blah" config
    let expected = "[Section 1]\n\
                    foo = bar # blah\n"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a comment on the same line, inserting a space between the key and the comment`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let config = Configuration.add options "Section 1" "foo" "bar" Configuration.empty
    let key = Configuration.getNode "Section 1" "foo" config
    let config = Configuration.addComment OnSameLine options key "blah" config
    let expected = "[Section 1]\n\
                    foo = bar # blah\n\
                    \n"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Adding a comment on the same line copyies the pre-existing trailing newline`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar\n\
                \n\
                beauty = truth"
    let config = Configuration.fromText options text
    let key = Configuration.getNode "Section 1" "foo" config
    let config = Configuration.addComment OnSameLine options key "blah" config
    let expected = "[Section 1]\n\
                    foo = bar # blah\n\
                    \n\
                    beauty = truth"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Adding a comment on the same line preserves leading whitespace`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar    \n\
                bar = baz"
    let config = Configuration.fromText options text
    let fooKey = Configuration.getNode "Section 1" "foo" config
    let config = Configuration.addComment OnSameLine options fooKey "blah" config
    let expected = "[Section 1]\n\
                    foo = bar    # blah\n\
                    bar = baz"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Adding a key after comment does not insert any blank lines`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar # blah\n"
    let config =
        text
        |> Configuration.fromText options
        |> Configuration.add options "Section 1" "bar" "baz"
    let actual = Configuration.toText options config
    let expected = "[Section 1]\n\
                    foo = bar # blah\n\
                    bar = baz\n"
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add comment on same line as section heading`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar"
    let config = Configuration.fromText options text
    let [section] = Configuration.getSectionNodes "Section 1" config
    let config = Configuration.addComment OnSameLine options section "blah" config
    let expected = "[Section 1] # blah\n\
                    foo = bar"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add comment before section heading`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar"
    let config = Configuration.fromText options text
    let [section] = Configuration.getSectionNodes "Section 1" config
    let config = Configuration.addComment OnPreviousLine options section "blah" config
    let expected = "# blah\n\
                    [Section 1]\n\
                    foo = bar"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add comment after section`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar"
    let config = Configuration.fromText options text
    let [section] = Configuration.getSectionNodes "Section 1" config
    let config = Configuration.addComment OnNextLine options section "blah" config
    let expected = "[Section 1]\n\
                    foo = bar\n\
                    # blah\n"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add comment before section`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar"
    let config = Configuration.fromText options text
    let [section] = Configuration.getSectionNodes "Section 1" config
    let config = Configuration.addComment OnPreviousLine options section "blah" config
    let expected = "# blah\n\
                    [Section 1]\n\
                    foo = bar"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add comment before global section`` () =
    let options =
        Options.defaultOptions
        |> Options.withNewlineRule LfNewline
        |> Options.withGlobalKeysRule AllowGlobalKeys
    let text = "foo = bar"
    let config = Configuration.fromText options text
    let [section] = Configuration.getSectionNodes "<global>" config
    let config = Configuration.addComment OnPreviousLine options section "blah" config
    let expected = "# blah\n\
                    foo = bar"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a comment before a comment`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                beauty = truth\n\
                \n\
                # Comment 1\n\
                # Comment 2\n\
                foo = bar"
    let config = Configuration.fromText options text
    let comments = List.map snd (Configuration.getComments "Section 1" "foo" config)
    let config = Configuration.addComment OnPreviousLine options (comments[0]) "Comment 0" config
    let expected = "[Section 1]\n\
                    beauty = truth\n\
                    \n\
                    # Comment 0\n\
                    # Comment 1\n\
                    # Comment 2\n\
                    foo = bar"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a comment before a key`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                beauty = truth\n\
                \n\
                # Comment 1\n\
                # Comment 2\n\
                foo = bar"
    let config = Configuration.fromText options text
    let key = Configuration.getNode "Section 1" "foo" config
    let config = Configuration.addComment OnPreviousLine options key "Comment 3" config
    let expected = "[Section 1]\n\
                    beauty = truth\n\
                    \n\
                    # Comment 1\n\
                    # Comment 2\n\
                    # Comment 3\n\
                    foo = bar"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a comment after a key`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                beauty = truth\n\
                \n\
                # Comment 1\n\
                # Comment 2\n\
                foo = bar"
    let config = Configuration.fromText options text
    let key = Configuration.getNode "Section 1" "foo" config
    let config = Configuration.addComment OnNextLine options key "Comment 3" config
    let expected = "[Section 1]\n\
                    beauty = truth\n\
                    \n\
                    # Comment 1\n\
                    # Comment 2\n\
                    foo = bar\n\
                    # Comment 3\n"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a comment before a multivalue key`` () =
    let options =
        Options.defaultOptions
        |> Options.withNewlineRule LfNewline
        |> Options.withDuplicateKeyRule DuplicateKeyAddsValue
    let text = "[Section 1]\n\
                # Comment 1\n\
                foo = 1\n\
                foo = 2\n\
                foo = 3"
    let config = Configuration.fromText options text
    let key = Configuration.getFirstNode "Section 1" "foo" config
    let config = Configuration.addComment OnPreviousLine options key "Comment 2" config
    let expected = "[Section 1]\n\
                    # Comment 1\n\
                    # Comment 2\n\
                    foo = 1\n\
                    foo = 2\n\
                    foo = 3"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a comment on same line as the Nth multivalue key`` () =
    let options =
        Options.defaultOptions
        |> Options.withNewlineRule LfNewline
        |> Options.withDuplicateKeyRule DuplicateKeyAddsValue
    let text = "[Section 1]\n\
                # Comment 1\n\
                foo = 1\n\
                foo = 2\n\
                foo = 3"
    let config = Configuration.fromText options text
    let key = Configuration.getNthNode "Section 1" "foo" 1 config
    let config = Configuration.addComment OnSameLine options key "Comment 2" config
    let expected = "[Section 1]\n\
                    # Comment 1\n\
                    foo = 1\n\
                    foo = 2 # Comment 2\n\
                    foo = 3"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a comment before the first node in a section`` () =
    let options =
        Options.defaultOptions
        |> Options.withNewlineRule LfNewline
    let text = "[Section 1]\n\
                # Just a comment\n\
                foo = bar"
    let config = Configuration.fromText options text
    let [_, comment] = Configuration.getComments "Section 1" "foo" config
    let config = Configuration.addComment OnPreviousLine options comment "Now this is first" config
    let expected = "[Section 1]\n\
                    # Now this is first\n\
                    # Just a comment\n\
                    foo = bar"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)

[<Fact>]
let ``Add a comment before the first node in a configuration`` () =
    let options = Options.defaultOptions.WithNewlineRule LfNewline
    let text = "[Section 1]\n\
                foo = bar"
    let config = Configuration.fromText options text
    let [section] = Configuration.getSectionNodes "Section 1" config
    let config = Configuration.addComment OnPreviousLine options section "Comment at the beginning" config
    let expected = "# Comment at the beginning\n\
                    [Section 1]\n\
                    foo = bar"
    let actual = Configuration.toText options config
    Assert.Equal(expected, actual)
