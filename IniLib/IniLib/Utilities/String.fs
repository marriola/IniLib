module IniLib.Utilities.String

let inline contains substring (s: string) = s.Contains(substring)

let inline endsWith substring (s: string) = s.EndsWith(substring)

let inline replace (substring: string) replacement (s: string) = s.Replace(substring, replacement)
