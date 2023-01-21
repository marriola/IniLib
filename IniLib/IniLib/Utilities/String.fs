module internal IniLib.Utilities.String

open System

let inline contains substring (s: string) = s.Contains(substring)

let inline endsWith substring (s: string) = s.EndsWith(substring)

let inline isEmpty s = String.IsNullOrEmpty s

let inline replace (substring: string) replacement (s: string) = s.Replace(substring, replacement)

let inline trim (s: string) = s.Trim()
