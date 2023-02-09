module IniLib.Test.LexerTests

open IniLib
open Xunit

[<Fact>]
let ``Lexer splits lines into separate tokens`` () =
    let options = Options.defaultOptions
    let text = "testing\n\
                123"
    let expected = [
        Text ("testing", At (0, 1, 1))
        Whitespace ("\n", At (7, 1, 8))
        Text ("123", At (8, 2, 1))
    ]
    let actual = Lexer.lex options text
    Assert.Equal<Token>(expected, actual)

[<Fact>]
let ``Lexer splits subsequent LF newlines into new tokens`` () =
    let options = Options.defaultOptions
    let text = "\n\n\n"
    let expected = [
        Whitespace ("\n", At (0, 1, 1))
        Whitespace ("\n", At (1, 2, 1))
        Whitespace ("\n", At (2, 3, 1))
    ]
    let actual = Lexer.lex options text
    Assert.Equal<Token>(expected, actual)

[<Fact>]
let ``Lexer splits subsequent CRLF newlines into new tokens`` () =
    let options = Options.defaultOptions
    let text = "\r\n\r\n\r\n"
    let expected = [
        Whitespace ("\r\n", At (0, 1, 1))
        Whitespace ("\r\n", At (2, 2, 1))
        Whitespace ("\r\n", At (4, 3, 1))
    ]
    let actual = Lexer.lex options text
    Assert.Equal<Token>(expected, actual)
