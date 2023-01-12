﻿module IniLib.Configuration

open IniLib
open IniLib.Utilities
open System
open System.Collections.Generic
open System.IO

type KeyMap = KeyMap of Map<string, (string * Node) list>
with
    static member (+) ((KeyMap map1), (KeyMap map2)) = KeyMap (Map.union map2 map1)

    static member unwrap (KeyMap map) = map

    member inline this.GetEntries keyName =
        let (KeyMap map) = this
        map[keyName]

    member this.Item
        with get keyName =
            List.map fst (this.GetEntries keyName)

    member this.GetNodes keyName =
        List.map snd (this.GetEntries keyName)

    member this.Keys () =
        let (KeyMap map) = this
        Map.keys map

type SectionMap = SectionMap of Map<string, KeyMap * Node list>
with
    static member unwrap (SectionMap map as config) =
        map

    member this.Item
        with get sectionName =
            let (SectionMap map) = this
            let section, _ = map[sectionName]
            section

    member this.GetNodes sectionName =
        let (SectionMap map) = this
        let _, node = map[sectionName]
        node

    member this.Keys () : IEnumerable<String> =
        let (SectionMap map) = this
        Map.keys map

type Configuration = Configuration of Node * SectionMap
with
    member this.Item
        with get sectionName =
            let (Configuration (_, dic)) = this
            let sectionDic = dic[sectionName]
            sectionDic

let empty = Configuration (RootNode [], SectionMap Map.empty)

let private toMap options (RootNode children) =
    let getKeys nodes =
        let rec getKeys' addedKeys out nodes =
            match nodes with
            | [] ->
                out
                |> List.rev
                |> List.groupBy fst
                |> List.map (fun (key, value) -> key, List.map snd value)
                |> Map.ofList
                |> KeyMap

            | KeyNode (name, value, _) as keyNode::rest ->
                let newEntry = (name, (value, keyNode))

                let nextOut =
                    match options.duplicateKeyRule with
                    | DuplicateKeyReplacesValue
                    | DisallowDuplicateKeys when Set.contains name addedKeys ->
                        List.replaceWhen (fst >> (=) name) newEntry out
                    | _ ->
                        newEntry :: out

                getKeys' (Set.add name addedKeys) nextOut rest

            | _::rest ->
                getKeys' addedKeys out rest

        getKeys' Set.empty [] nodes

    let rec getSections out nodes =
        match nodes with
        | [] ->
            out
            |> List.rev
            |> Map.ofList
            |> SectionMap

        | SectionNode (name, children) as sectionNode::rest ->
            let getSectionName = fst
            let nextOut =
                if List.exists (getSectionName >> (=) name) out then
                    // Add keys for this section to existing map
                    out
                    |> List.map (fun ((n, (keys1, nodes1)) as entry) ->
                        if n = name then
                            let keys2 = getKeys children
                            (name, (keys1 + keys2, sectionNode :: nodes1))
                        else
                            entry)
                else
                    // Create map for this section
                    (name, (getKeys children, [ sectionNode ])) :: out
            getSections nextOut rest

        | _::rest ->
            getSections out rest

    getSections [] children

/// The section keys of the configuration.
let sections config =
    let (Configuration (_, dic)) = config
    dic.Keys()

/// The keys of the section.
let keys sectionName config =
    let (Configuration (_, dic)) = config
    let sectionDic = dic[sectionName]
    sectionDic.Keys()

let private tryGetMultivalueKey sectionName keyName (Configuration (_, dic)) =
    dic
    |> SectionMap.unwrap
    |> Map.tryFind sectionName
    |> Option.bind (fst >> KeyMap.unwrap >> Map.tryFind keyName)

let private tryGetKey sectionName keyName config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.last)

let private tryGetFirstKey sectionName keyName config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.item 0)

/// Looks up the value of a key in the configuration syntax tree, returning Some if the key is found and None if not.
let tryGet sectionName keyName config =
    tryGetKey sectionName keyName config
    |> Option.map fst

/// Looks up the value of a key in the configuration syntax tree, returning Some if the key is found and None if not.
/// If the key is a multivalue key, the last value is returned.
let tryGetFirst sectionName keyName config =
    tryGetFirstKey sectionName keyName config
    |> Option.map fst

/// Looks up the values of a multivalue key in the configuration syntax tree, returning Some if the key is found and None if not.
/// Returns a singleton list if multivalue keys are not allowed.
let tryGetMultiValues sectionName keyName config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.map fst)

/// Looks up the integer value of a key in the configuration syntax tree, returning Some if the key is found and None if not.
let tryGetInt sectionName keyName config =
    tryGet sectionName keyName config
    |> Option.map int32

/// Looks up the integer value of a key in the configuration syntax tree, returning Some if the key is found and None if not.
/// If the key is a multivalue key, the last value is returned.
let tryGetFirstInt sectionName keyName config =
    tryGetFirst sectionName keyName config
    |> Option.map int32

/// Looks up the integer value of a key in the configuration syntax tree, returning Some if the key is found and None if not.
let tryGetMultiValueInts sectionName keyName config =
    tryGetMultiValues sectionName keyName config
    |> Option.map (List.map int32)

/// Looks up the node of a key in the configuration syntax tree, returning Some if the key is found and None if not.
let tryGetNode sectionName keyName config =
    tryGetKey sectionName keyName config
    |> Option.map snd

/// Looks up the node of a key in the configuration syntax tree, returning Some if the key is found and None if not.
/// If the key is a multivalue key, the last value is returned.
let tryGetFirstNode sectionName keyName config =
    tryGetFirstKey sectionName keyName config
    |> Option.map snd

/// Looks up the nodes of a multivalue key in the configuration syntax tree, returning Some if the key is found and None if not.
let tryGetMultiValueNodes sectionName keyName config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.map snd)

let tryGetSection sectionName config =
    let (Configuration (_, dic)) = config
    dic
    |> SectionMap.unwrap
    |> Map.tryFind sectionName

/// Looks up the node of a section in the configuration syntax tree, returning Some if the section is found and None if not.
let tryGetSectionNode sectionName config =
    Option.map snd (tryGetSection sectionName config)

/// Looks up the value of a key in the configuration.
let get sectionName keyName config =
    tryGet sectionName keyName config
    |> Option.get

/// Looks up the value of a key in the configuration.
/// If the key is a multivalue key, the last value is returned.
let getFirst sectionName keyName config =
    tryGetFirst sectionName keyName config
    |> Option.get

/// Looks up the values of a multivalue key in the configuration. Returns a singleton list if multivalue keys are not allowed.
let getMultiValues sectionName keyName config =
    tryGetMultiValues sectionName keyName config
    |> Option.get

/// Looks up the nodes of a multivalue key in the configuration syntax tree, returning Some if the key is found and None if not.
let getMultiValueNodes sectionName keyName config =
    tryGetMultiValueNodes sectionName keyName config
    |> Option.get

/// Looks up the integer value of a key in the configuration.
let getInt sectionName keyName config =
    tryGetInt sectionName keyName config
    |> Option.get

/// Looks up the integer value of a key in the configuration.
/// If the key is a multivalue key, the last value is returned.
let getFirstInt sectionName keyName config =
    tryGetFirstInt sectionName keyName config
    |> Option.get

/// Looks up the node of a key in the configuration syntax tree.
let getNode sectionName keyName config =
    tryGetNode sectionName keyName config
    |> Option.get

/// Looks up the node of a key in the configuration syntax tree.
/// If the key is a multivalue key, the last value is returned.
let getFirstNode sectionName keyName config =
    tryGetFirstNode sectionName keyName config
    |> Option.get

/// Looks up the node of a section in the configuration syntax tree.
let getSectionNode sectionName config =
    tryGetSectionNode sectionName config
    |> Option.get

let private RE_LEADING_WHITESPACE = new System.Text.RegularExpressions.Regex("^(\\s+)")

/// Returns a new configuration with the key added.
let add options sectionName keyName value config =
    let (Configuration (tree, dic)) = config

    let getKeyNodeChildren = function
        | KeyNode (_, _, KeyNameNode _ :: TokenNode (Assignment _) :: KeyValueNode (_, children) :: _)
        | KeyNode (_, _, KeyNameNode _ :: KeyValueNode (_, children) :: _) ->
            children

    let replace sectionName keyName value =
        // Single key already exists, replace value and existing text nodes
        let section = dic.[sectionName]
        let keyNode::_ = section.GetNodes(keyName)
        let target = List.filter Node.isReplaceable (getKeyNodeChildren keyNode)

        // Create new token node and replace original nodes
        let (KeyValueNode (_, keyValueChildren)) = NodeBuilder.keyValue options value
        let newText = List.filter Node.isReplaceable keyValueChildren
        let newTree = Node.replace Node.isReplaceable options target newText tree
        let newDic = toMap options newTree

        Configuration (newTree, newDic)

    let addToSection (SectionNode (sectionName, children) as sectionNode) keyNode =
        // If multivalue keys are allowed, add after the last key of the same name.
        // If not found, or if multivalue keys are not allowed, add after the last key.
        let lastMatchingKeyIndex =
            match options.duplicateKeyRule with
            | DuplicateKeyAddsValue ->
                List.tryFindIndexBack
                    (function KeyNode (kn, _, _) when keyName = kn -> true | _ -> false)
                    children
            | _ ->
                None

        let lastKeyIndex =
            lastMatchingKeyIndex
            |> Option.orElseWith (fun () -> List.tryFindIndexBack (function KeyNode _ -> true | _ -> false) children)
            |> Option.defaultValue -1

        let lastKeyText = if lastKeyIndex = -1 then "" else Node.toText options children.[lastKeyIndex]

        // Insert a newline if the last key didn't end in a newline
        let newline = if lastKeyText.EndsWith("\n") then [] else [ NodeBuilder.newlineTrivia options ]

        // Copy leading whitespace from last key
        let keyNode =
            let regexMatch = RE_LEADING_WHITESPACE.Match(lastKeyText)
            if regexMatch.Success then
                let whitespaceNode = NodeBuilder.whitespaceTrivia regexMatch.Groups[0].Value
                let (KeyNode (name, value, (KeyNameNode _ as keyNameNode)::rest)) = keyNode
                let keyNameNode = Node.addChildToBeginning whitespaceNode keyNameNode
                KeyNode (name, value, [keyNameNode] @ rest)
            else
                keyNode

        let newSectionChildren = List.collect id [
            children.[0..lastKeyIndex]
            newline
            [ keyNode ]
            children.[lastKeyIndex + 1..]
        ]

        let newSectionNode = SectionNode (sectionName, newSectionChildren)
        let newTree = Node.replace ((=) sectionNode) options [sectionNode] [newSectionNode] tree
        let newDic = toMap options newTree

        Configuration (newTree, newDic)

    let addSectionWithKey sectionName keyNode =
        let (RootNode children) = tree
        let nodesOut = NodeBuilder.section options sectionName [ keyNode ]
        let newChildren =
            if sectionName = "<global>" && options.globalKeysRule = AllowGlobalKeys then
                // Insert global section after initial comments, before all other sections
                let comments = List.takeWhile (function TriviaNode _ | CommentNode _ -> true | _ -> false) children
                comments @ [ nodesOut ] @ [ NodeBuilder.newlineTrivia options ] @ children[comments.Length..]
            else
                // Insert a newline if the last node doesn't end with a newline, and add the section at the end
                let lastChildText =
                    children
                    |> List.tryLast
                    |> Option.map (Node.toText options)
                    |> Option.defaultValue "\n"
                let newline = if lastChildText.EndsWith("\n") then [] else [ NodeBuilder.newlineTrivia options ]
                children @ newline @ [ nodesOut ]

        let newTree = RootNode newChildren
        let newDic = toMap options newTree
        Configuration (newTree, newDic)

    let section = Map.tryFind sectionName (SectionMap.unwrap dic)
    let key = Option.bind (fst >> KeyMap.unwrap >> Map.tryFind keyName) section

    match key with
    | Some _ ->
        match options.duplicateKeyRule with
        | DisallowDuplicateKeys
        | DuplicateKeyReplacesValue -> replace sectionName keyName value
        | DuplicateKeyAddsValue -> addToSection (section |> Option.get |> snd |> List.last) (NodeBuilder.key options keyName value)
    | None ->
        // Key doesn't exist, create new key node
        let keyNode = NodeBuilder.key options keyName value
        match section with
        | Some (_, sectionNodes) -> addToSection (List.last sectionNodes) keyNode
        | None -> addSectionWithKey sectionName keyNode

/// Returns a new configuration with the key removed.
let removeKey options sectionName keyName config =
    let (Configuration (tree, dic)) = config
    let sectionDic = dic[sectionName]
    let keys = sectionDic.GetEntries keyName
    // Delete keys in reverse so that changing line positions don't ruin equivalence and make us miss the target keys
    let newTree =
        keys
        |> List.rev
        |> List.fold (fun tree (_, keyNode) -> Node.replace ((=) keyNode) options [keyNode] [] tree) tree
    let newDic = toMap options newTree
    
    Configuration (newTree, newDic)

/// Returns a new configuration with the section removed.
let removeSection options sectionName config =
    let (Configuration (tree, dic)) = config
    let sectionNodes = dic.GetNodes sectionName
    let newTree = List.fold (fun tree sectionNode -> Node.replace ((=) sectionNode) options [sectionNode] [] tree) tree sectionNodes
    let newDic = toMap options newTree
    Configuration (newTree, newDic)

/// Returns a new configuration with the section renamed.
let renameSection options sectionName newName config =
    // Get the section
    let (Configuration (tree, dic)) = config
    let sectionNodes = dic.GetNodes sectionName

    let newTree =
        (tree, sectionNodes)
        ||> List.fold (fun tree sectionNode ->
            // Get section heading name nodes
            let sectionHeadingText =
                match sectionNode with
                | SectionNode (name, SectionHeadingNode (_, children)::_) ->
                    List.filter Node.isReplaceable children

            let (SectionNode (_, sectionChildren)) = sectionNode

            // Replace section heading name and rebuild configuration
            let newText = [ NodeBuilder.replaceableText newName ]
            let newSection = SectionNode (newName, sectionChildren)
            let newSection = Node.replace Node.isReplaceable options sectionHeadingText newText newSection
            Node.replace ((=) sectionNode) options [sectionNode] [newSection] tree)

    let newDic = toMap options newTree
    
    Configuration (newTree, newDic)

/// Returns a new configuration with the key renamed.
let renameKey options sectionName keyName newKeyName config =
    // Get the section
    let (Configuration (tree, dic)) = config
    let sectionNodes = dic.GetNodes sectionName

    // Replace key name in each matching section node and rebuild configuration
    let newTree =
        (tree, sectionNodes)
        ||> List.fold (fun tree sectionNode ->
            let children = Node.getChildren sectionNode
            let newText = [ NodeBuilder.replaceableText newKeyName ]

            // Replace name in matching keys
            (tree, children)
            ||> List.fold
                (fun tree node ->
                    match node with
                    | KeyNode (oldName, _, KeyNameNode (_, children)::_) when oldName = keyName ->
                        let keyNameText = List.filter Node.isReplaceable children
                        Node.replace Node.isReplaceable options keyNameText newText tree
                    | _ ->
                        tree))
            
    Configuration (newTree, toMap options newTree)

let ofList options xs = List.fold (fun config (section, key, value) -> add options section key value config) empty xs
let ofSeq options seq = Seq.fold (fun config (section, key, value) -> add options section key value config) empty seq

/// Parses a configuration from text.
let fromText options text =
    let tree =
        text
        |> Lexer.lex options
        |> Parser.parse options

    Configuration (tree, toMap options tree)

#if !FABLE_COMPILER

/// Parses a configuration from a stream.
let fromStream options (stream: Stream) =
    use reader = new StreamReader(stream)
    fromText options (reader.ReadToEnd())

/// Parses a configuration from a stream reader.
let fromStreamReader options (reader: StreamReader) =
    fromText options (reader.ReadToEnd())

/// Parses a configuration from a text reader.
let fromTextReader options (reader: TextReader) =
    fromText options (reader.ReadToEnd())

/// Parses a configuration from a file.
let fromFile options path =
    path
    |> File.ReadAllText
    |> fromText options

#endif

/// Converts all assignment tokens and the surrounding spacing if they disagree with the applied name value delimiter preference and spacing rules
let private convertNameValueDelimiters options (RootNode children) =
    let convertSection (SectionNode (name, sectionChildren)) =
        let convertKey name value maybeAssignmentChar keyNode (KeyNameNode (_, keyNameChildren)) (KeyValueNode (_, keyValueChildren)) rest =
            let preferredAssignmentChar =
                match options.nameValueDelimiterPreferenceRule with
                | PreferEqualsDelimiter -> Some '='
                | PreferColonDelimiter -> Some ':'
                | PreferNoDelimiter -> None

            if preferredAssignmentChar = maybeAssignmentChar
                || options.nameValueDelimiterRule = EqualsOrColonDelimiter && maybeAssignmentChar <> None then
                keyNode
            else
                let keyNameNode =
                    let leftWhitespace =
                        match options.nameValueDelimiterSpacingRule with
                        | BothSides | LeftOnly -> [ NodeBuilder.whitespaceTrivia " " ]
                        | _ -> []
                    let keyNameNodeStripped =
                        keyNameChildren
                        |> List.rev
                        |> List.skipWhile Node.isWhitespace
                        |> List.rev
                    let node = KeyNameNode (name, keyNameNodeStripped @ leftWhitespace)
                    if options.nameValueDelimiterRule = NoDelimiter then NodeBuilder.sanitize options node else node
                let assignmentNode =
                    match preferredAssignmentChar with
                    | Some c -> [ TokenNode (Assignment (c, 0, 0)) ]
                    | None -> []
                let keyValueNode =
                    let rightWhitespace =
                        match options.nameValueDelimiterSpacingRule with
                        | BothSides | RightOnly -> [ NodeBuilder.whitespaceTrivia " " ]
                        | _ -> []
                    let keyValueNodeStripped =
                        keyValueChildren
                        |> List.skipWhile Node.isWhitespace
                    KeyValueNode (value, rightWhitespace @ keyValueNodeStripped)
                KeyNode (name, value, [ keyNameNode ] @ assignmentNode @ [ keyValueNode ] @ rest)
                
        let newSectionChildren =
            sectionChildren
            |> List.map (function
                | KeyNode (name, value, (KeyNameNode _ as keyNameNode)::(KeyValueNode _ as keyValueNode)::rest) as keyNode ->
                    convertKey name value None keyNode keyNameNode keyValueNode rest

                | KeyNode (name, value, (KeyNameNode _ as keyNameNode)::(TokenNode (Assignment (c, _, _)))::(KeyValueNode _ as keyValueNode)::rest) as keyNode ->
                    convertKey name value (Some c) keyNode keyNameNode keyValueNode rest

                | node ->
                    node)
        SectionNode (name, newSectionChildren)
    let newChildren =
        children
        |> List.map (function
            | SectionNode _ as sectionNode -> convertSection sectionNode
            | node -> node)
    RootNode newChildren

/// Converts the configuration to text.
let toText options (Configuration (tree, _)) =
    let newTree = convertNameValueDelimiters options tree
    Node.toText options newTree

#if !FABLE_COMPILER

/// Writes a configuration to a file.
let writeToFile options (path: string) config =
    use streamWriter = new StreamWriter(path)
    streamWriter.Write(toText options config)

/// Writes a configuration to a stream writer.
let writeToStreamWriter options (streamWriter: StreamWriter) config =
    streamWriter.Write(toText options config)

/// Writes a configuration to a text writer.
let writeToTextWriter options (textWriter: TextWriter) config =
    textWriter.Write(toText options config)

/// Writes a configuration to a stream.
let writeToStream options (encoding: System.Text.Encoding) (stream: Stream) config =
    let text = toText options config
    let buffer = encoding.GetBytes(text)
    stream.Write(buffer, 0, buffer.Length)

#endif
