module IniLib.Utilities.Map

let inline union map1 map2 =
    let map1Keys = Map.toSeq map1
    let map2Keys = Map.toSeq map2
    seq { map2Keys; map1Keys }
    |> Seq.concat
    |> Map.ofSeq
