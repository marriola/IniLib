module IniLib.Test.LexerTests

open IniLib
open Xunit

[<Fact>]
let ``Lexer splits lines into separate tokens`` () =
    let options = Options.defaultOptions
    let text = "testing\n\
                123"
    let expected = [ Text ("testing", 1, 1); Whitespace ("\n", 1, 8); Text ("123", 2, 1) ]
    let actual = Lexer.lex options text
    Assert.Equal<Token>(expected, actual)

[<Fact>]
let ``Lexer splits subsequent LF newlines into new tokens`` () =
    let options = Options.defaultOptions
    let text = "\n\n\n"
    let expected = [ Whitespace ("\n", 1, 1); Whitespace ("\n", 2, 1); Whitespace ("\n", 3, 1) ]
    let actual = Lexer.lex options text
    Assert.Equal<Token>(expected, actual)

[<Fact>]
let ``Lexer splits subsequent CRLF newlines into new tokens`` () =
    let options = Options.defaultOptions
    let text = "\r\n\r\n\r\n"
    let expected = [ Whitespace ("\r\n", 1, 1); Whitespace ("\r\n", 2, 1); Whitespace ("\r\n", 3, 1) ]
    let actual = Lexer.lex options text
    Assert.Equal<Token>(expected, actual)
