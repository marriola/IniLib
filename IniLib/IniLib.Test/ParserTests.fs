module IniLib.Test.ParserTests

open IniLib
open Xunit

[<Fact>]
let ``Whitespace not included in key value`` () =
    let options = Options.defaultOptions.WithGlobalKeysRule(AllowGlobalKeys).WithNameValueDelimiterRule(ColonDelimiter).WithQuotationRule(UseQuotation)
    let text = "foo: bar\n\
                prince vegeta: \"nine thousand\"\n\
                \n"
    let config = Configuration.fromText options text
    let actual = Configuration.get "<global>" "foo" config
    let expected = "bar"
    Assert.Equal(expected, actual)
