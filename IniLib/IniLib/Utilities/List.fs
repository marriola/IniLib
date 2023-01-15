module internal IniLib.Utilities.List

let inline deleteAt i (xs: 'a list) = xs[..i-1] @ xs[i+1..]

let inline ofOption opt =
    match opt with
    | Some value -> [ value ]
    | None -> []

let inline replace original replacement xs = List.map (fun x -> if x = original then replacement else x) xs

let inline replaceWhen predicate replacement xs = List.map (fun x -> if predicate x then replacement else x) xs
