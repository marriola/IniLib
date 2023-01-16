[<AutoOpen>]
module internal IniLib.Utilities.ActivePatterns

open System.Text.RegularExpressions

let (|RegexMatch|_|) (pattern: Regex) (text: string) =
    let m = pattern.Match text
    if m.Success then
        Some m.Groups[1]
    else
        None

let (|RegexMatches|_|) (pattern: Regex) (text: string) =
    let m = pattern.Match text
    if m.Success then
        Some m.Groups
    else
        None
