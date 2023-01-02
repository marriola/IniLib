﻿module IniLib.Utilities.List

let inline deleteAt i (xs: 'a list) = xs[..i-1] @ xs[i+1..]

let inline replace original replacement = List.map (fun x -> if x = original then replacement else x)
