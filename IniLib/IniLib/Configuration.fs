module IniLib.Configuration

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

/// The empty configuration.
let empty = Configuration (RootNode [], SectionMap Map.empty)

let private toMap options tree =
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
                            (name, (keys1 + keys2, nodes1 @ [ sectionNode ]))
                        else
                            entry)
                else
                    // Create map for this section
                    (name, (getKeys children, [ sectionNode ])) :: out
            getSections nextOut rest

        | _::rest ->
            getSections out rest

    getSections [] (Node.getChildren tree)

/// Returns the section keys of the configuration.
let sections config =
    let (Configuration (_, dic)) = config
    dic.Keys()

/// Returns the keys of the section.
let keys sectionName config =
    let (Configuration (_, dic)) = config
    let sectionDic = dic[sectionName]
    sectionDic.Keys()

/// Returns all entries for a multivalue key if found in the configuration, otherwise returns None.
let private tryGetMultivalueKey sectionName keyName (Configuration (_, dic)) =
    dic
    |> SectionMap.unwrap
    |> Map.tryFind sectionName
    |> Option.bind (fst >> KeyMap.unwrap >> Map.tryFind keyName)

/// Returns the key entry if found in the configuration, otherwise returns None.
/// If duplicate keys are allowed, the last entry is returned.
let private tryGetKey sectionName keyName config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.last)

/// Returns the first key entry if found in the configuration, otherwise returns None.
let private tryGetFirstKey sectionName keyName config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.item 0)

/// Returns the Nth key entry of a multivalue key if found in the configuration, otherwise returns None.
let private tryGetNthKey sectionName keyName index config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.item index)

/// Returns the value of a key if found in the configuration, otherwise returns None.
/// If duplicate keys are allowed, the last value is returned.
let tryGet sectionName keyName config =
    tryGetKey sectionName keyName config
    |> Option.map fst

/// Returns the value of a key if found in the configuration, otherwise returns None.
let tryGetFirst sectionName keyName config =
    tryGetFirstKey sectionName keyName config
    |> Option.map fst

/// Returns the Nth value of a multivalue key if found in the configuration, otherwise returns None.
let tryGetNth sectionName keyName index config =
    tryGetNthKey sectionName keyName index config
    |> Option.map fst

/// Returns the values of a multivalue key if found in the configuration, otherwise returns None.
/// Returns a singleton list if multivalue keys are not allowed.
let tryGetMultiValues sectionName keyName config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.map fst)

/// Returns the integer value of a key in the configuration, returning Some if the key is found and None if not.
/// If duplicate keys are allowed, the last value is returned.
let tryGetInt sectionName keyName config =
    tryGet sectionName keyName config
    |> Option.map int32

/// Returns the integer value of a key if found in the configuration, otherwise returns None.
let tryGetFirstInt sectionName keyName config =
    tryGetFirst sectionName keyName config
    |> Option.map int32

/// Returns the Nth integer value of a multivalue key if found in the configuration, otherwise returns None.
let tryGetNthInt sectionName keyName index config =
    tryGetNth sectionName keyName index config
    |> Option.map int32

/// Returns the integer values of a key if found in the configuration, otherwise returns None.
let tryGetMultiValueInts sectionName keyName config =
    tryGetMultiValues sectionName keyName config
    |> Option.map (List.map int32)

/// Returns the node of a key if found in the configuration, otherwise returns None.
/// If duplicate keys are allowed, the last node is returned.
let tryGetNode sectionName keyName config =
    tryGetKey sectionName keyName config
    |> Option.map snd

/// Returns the first node of a key if found in the configuration, otherwise returns None.
let tryGetFirstNode sectionName keyName config =
    tryGetFirstKey sectionName keyName config
    |> Option.map snd

/// Returns the Nth node of a multivalue key if found in the configuration, otherwise returns None.
let tryGetNthNode sectionName keyName index config =
    tryGetNthKey sectionName keyName index config
    |> Option.map snd

/// Returns the nodes of a multivalue key if found in the configuration, otherwise returns None.
let tryGetMultiValueNodes sectionName keyName config =
    tryGetMultivalueKey sectionName keyName config
    |> Option.map (List.map snd)

/// Returns the section entry if found in the configuration, otherwise returns None.
let tryGetSection sectionName config =
    let (Configuration (_, dic)) = config
    dic
    |> SectionMap.unwrap
    |> Map.tryFind sectionName

/// Returns the section nodes identified by the given section name if found in the configuration, otherwise returns None.
let tryGetSectionNodes sectionName config =
    Option.map snd (tryGetSection sectionName config)

/// Returns all comments on the immediately preceding lines or on the same line as the node if found in the tree, otherwise
/// returns None.
let tryGetComments targetNode (Configuration (tree, _) as config) =
    let rec tryGetComments' lastComments out nodes =
        match nodes with
        | [] ->
            List.rev out
        | SectionHeadingNode _::rest | TriviaNode _::rest ->
            tryGetComments' [] out rest
        | CommentNode _ as commentNode::rest ->
            tryGetComments' (commentNode :: lastComments) out rest
        | node::rest when node = targetNode ->
            let sameLineComment = Node.tryFindChild Node.isComment node
            let nextOut = lastComments @ out
            let nextOut =
                match sameLineComment with
                | Some comment -> comment :: nextOut
                | None -> nextOut
            tryGetComments' [] nextOut rest
        | _::rest ->
            tryGetComments' [] out rest

    match Node.findParent tree targetNode with
    | None -> None
    | Some parent ->
        parent
        |> Node.getChildren
        |> tryGetComments' [] []
        |> List.map (function CommentNode (text, _) as node -> text, node)
        |> Some

/// Returns all comments on the preceding lines and the same line as the key if found in the section, otherwise returns None.
let tryGetKeyComments sectionName keyName config =
    let getNodeComments n = Option.defaultValue [] (tryGetComments n config)
    let getAllComments =
        Node.getChildren
        >> List.filter (Node.getText1 >> (=) keyName)
        >> List.collect getNodeComments
    config
    |> tryGetSectionNodes sectionName
    |> Option.map (List.collect getAllComments)

/// Returns the value of a key in the configuration.
/// If duplicate keys are allowed, the last value is returned.
let get sectionName keyName config =
    tryGet sectionName keyName config
    |> Option.get

/// Returns the first value of a key in the configuration.
let getFirst sectionName keyName config =
    tryGetFirst sectionName keyName config
    |> Option.get

/// Returns the Nth value of a multivalue key in the configuration.
let getNth sectionName keyName index config =
    tryGetNth sectionName keyName index config
    |> Option.get

/// Returns the values of a multivalue key in the configuration. Returns a singleton list if multivalue keys are not allowed.
let getMultiValues sectionName keyName config =
    tryGetMultiValues sectionName keyName config
    |> Option.get

/// Returns the nodes of a multivalue key in the configuration syntax tree, returning Some if the key is found and None if not.
let getMultiValueNodes sectionName keyName config =
    tryGetMultiValueNodes sectionName keyName config
    |> Option.get

/// Returns the integer value of a key in the configuration.
/// If duplicate keys are allowed, the last value is returned.
let getInt sectionName keyName config =
    tryGetInt sectionName keyName config
    |> Option.get

/// Returns the integer value of a key in the configuration.
let getFirstInt sectionName keyName config =
    tryGetFirstInt sectionName keyName config
    |> Option.get

/// Returns the Nth integer value of a multivalue key in the configuration.
let getFirstNth sectionName keyName index config =
    tryGetNthInt sectionName keyName index config
    |> Option.get

/// Returns the node of a key in the configuration syntax tree.
/// If duplicate keys are allowed, the last value is returned.
let getNode sectionName keyName config =
    tryGetNode sectionName keyName config
    |> Option.get

/// Returns the node of a key in the configuration syntax tree.
let getFirstNode sectionName keyName config =
    tryGetFirstNode sectionName keyName config
    |> Option.get

/// Returns the Nth node of a multivalue key in the configuration syntax tree.
let getNthNode sectionName keyName index config =
    tryGetNthNode sectionName keyName index config
    |> Option.get

/// Returns the nodes of a section in the configuration syntax tree.
let getSectionNodes sectionName config =
    tryGetSectionNodes sectionName config
    |> Option.get

/// Returns the key and returns all comments on the previous lines and the same line.
let getComments sectionName keyName config =
    tryGetKeyComments sectionName keyName config
    |> Option.get

let private RE_LEADING_WHITESPACE = new System.Text.RegularExpressions.Regex("^(\\s+)")

/// Returns a new configuration with the key added.
let add options sectionName keyName value config =
    let (Configuration (tree, dic)) = config

    let replace sectionName keyName value =
        // Single key already exists, replace value and existing text nodes
        let section = dic.[sectionName]
        let keyNodes = section.GetNodes(keyName)
        let target =
            keyNodes
            |> List.last
            |> Node.findChild (function KeyValueNode _ -> true | _ -> false)
            |> Node.findChildren Node.isReplaceable

        // Create new token node and replace original nodes
        let newText =
            value
            |> NodeBuilder.keyValue options
            |> Node.findChildren Node.isReplaceable
        let newTree = Node.replace Node.isReplaceable options target newText tree
        Configuration (newTree, toMap options newTree)

    let addToSection sectionNode keyNode =
        let children = Node.getChildren sectionNode

        // If multivalue keys are allowed, add after the last key of the same name.
        // If not found, or if multivalue keys are not allowed, add after the last key.
        let lastMatchingKeyIndex =
            match options.duplicateKeyRule with
            | DuplicateKeyAddsValue -> List.tryFindIndexBack (function KeyNode (kn, _, _) when keyName = kn -> true | _ -> false) children
            | _ -> None

        // If there are no keys, try the last comment, then the section heading.
        // There should always be a section heading, since if global keys are allowed,
        // and the configuration is empty, there is nothing to parse to make it go into this branch.
        let insertIndex, previousNode =
            lastMatchingKeyIndex
            |> Option.orElseWith (fun () -> List.tryFindIndexBack (function KeyNode _ -> true | _ -> false) children)
            |> Option.orElseWith (fun () -> List.tryFindIndexBack (function CommentNode _ | SectionHeadingNode _ -> true | _ -> false) children)
            |> Option.map (fun i -> i, children[i])
            |> Option.get

        // Insert a newline if the last key didn't end in a newline, and copy leading whitespace from last key
        let _, trailingNewline =
            previousNode
            |> Node.getChildren
            |> Node.splitTrailingWhitespace (Node.toText options >> String.endsWith "\n")
        let newline = if trailingNewline.Length > 0 then [] else [ NodeBuilder.newlineTrivia options ]
        let outNodes = newline @ [ Node.copyLeadingWhitespace previousNode keyNode ]

        let newSectionNode = Node.insertChildrenAt (insertIndex + 1) outNodes sectionNode
        let newTree = Node.replace ((=) sectionNode) options [sectionNode] [newSectionNode] tree
        Configuration (newTree, toMap options newTree)

    let addSectionWithKey sectionName keyNode =
        let children = Node.getChildren tree
        let sectionNode = NodeBuilder.section options sectionName [ keyNode ]
        let newChildren =
            let newlineTrivia = NodeBuilder.newlineTrivia options
            if sectionName = "<global>" && options.globalKeysRule = AllowGlobalKeys then
                // Insert global section after initial comments, before all other sections
                let nonCommentIndex =
                    children
                    |> List.tryFindIndex (function TriviaNode _ | CommentNode _ -> false | _ -> true)
                    |> Option.defaultValue 0
                List.insertManyAt nonCommentIndex [ sectionNode; newlineTrivia ] children
            else
                // Copy leading whitespace from last section
                let (Configuration (tree, _)) = config
                let lastSectionChildren = 
                    tree
                    |> Node.getChildren
                    |> List.tryFindBack (function SectionNode _ -> true | _ -> false)
                    |> Option.map Node.getChildren
                    |> Option.defaultValue []
                let newKeyNode =
                    lastSectionChildren
                    |> List.tryFindBack (function KeyNode _ | CommentNode _ | SectionHeadingNode _ -> true | _ -> false)
                    |> Option.map (fun previousNode -> Node.copyLeadingWhitespace previousNode keyNode)
                    |> Option.defaultValue keyNode
                let newSectionNode = Node.replace ((=) keyNode) options [keyNode] [newKeyNode] sectionNode

                // Insert a newline if the last node doesn't end with a newline, and add the section at the end
                let newline =
                    children
                    |> List.tryLast
                    |> Option.map (fun n -> if Node.endsWith options "\n" n then [] else [ newlineTrivia; newlineTrivia ])
                    |> Option.defaultValue []
                children @ newline @ [ newSectionNode ]

        let newTree = Node.renumber options (RootNode newChildren)
        Configuration (newTree, toMap options newTree)

    let section = Map.tryFind sectionName (SectionMap.unwrap dic)
    let key = Option.bind (fst >> KeyMap.unwrap >> Map.tryFind keyName) section

    match section, key with
    | Some (_, sectionNodes), Some _ ->
        match options.duplicateKeyRule with
        | DisallowDuplicateKeys
        | DuplicateKeyReplacesValue ->
            replace sectionName keyName value
        | DuplicateKeyAddsValue ->
            addToSection (List.last sectionNodes) (NodeBuilder.key options keyName value)
    | Some (_, sectionNodes), None ->
        addToSection (List.last sectionNodes) (NodeBuilder.key options keyName value)
    | None, None ->
        addSectionWithKey sectionName (NodeBuilder.key options keyName value)

/// Returns a new configuration with the text of the comment replaced.
let changeComment options text (_, commentNode) (Configuration (tree, _) as config) =
    let commentText = Node.findChildren Node.isReplaceable commentNode
    let newText = NodeBuilder.replaceableText text
    let newTree = Node.replace Node.isReplaceable options commentText [ newText ] tree
    Configuration (newTree, toMap options newTree)

/// <summary>
/// Returns a new configuration with a comment inserted next to another node.
/// </summary>
/// <param name="commentPosition">The relative position to add the comment.</param>
/// <param name="options">The configuration options.</param>
/// <param name="targetNode">The node to attach the comment to.</param>
/// <param name="text">The text of the comment to add.</param>
/// <param name="config">The configuration to modify.</param>
let addComment commentPosition options targetNode text (Configuration (tree, _) as config) =
    let commentNode = NodeBuilder.comment options text
    let targetNode =
        match commentPosition, targetNode with
        | OnSameLine, SectionNode (_, (SectionHeadingNode _ as sectionHeadingNode)::_) -> sectionHeadingNode
        | _ -> targetNode

    let next fContinue nextPosition children =
        match List.tryFindIndex ((=) targetNode) children with
        | None ->
            fContinue nextPosition children
        | Some targetIndex ->
            let nextChildren =
                match commentPosition, targetNode with
                | OnPreviousLine, _ ->
                    let commentNode =
                        commentNode
                        |> Node.copyLeadingWhitespace targetNode
                        |> Node.appendChild (NodeBuilder.newlineTrivia options)
                    List.insertAt targetIndex commentNode children
                | OnNextLine, _ ->
                    let commentNode =
                        commentNode
                        |> Node.copyLeadingWhitespace targetNode
                        |> Node.appendChild (NodeBuilder.newlineTrivia options)
                    let nextIndex = min (targetIndex + 1) (List.length children)
                    let targetNodeWithNewline = NodeBuilder.addNewlineIfNeeded options targetNode
                    children
                    |> List.replace targetNode targetNodeWithNewline
                    |> List.insertAt nextIndex commentNode
                | OnSameLine, SectionNode ("<global>", _) -> 
                    commentNode :: children
                | OnSameLine, _ ->
                    let targetChildren, _ =
                        targetNode
                        |> Node.getChildren
                        |> List.filter (Node.isNotComment)
                        |> Node.splitTrailingWhitespace (Node.endsWith options "\n")
                    let lastNodeText = targetChildren |> List.last |> Node.toText options
                    let space = if String.endsWith " " lastNodeText || String.endsWith "\t" lastNodeText then [] else [ NodeBuilder.spaceTrivia() ]
                    let nextTargetNode =
                        targetNode
                        |> Node.withChildren (targetChildren @ space @ [ commentNode ])
                        |> NodeBuilder.addNewlineIfNeeded options
                    List.replace targetNode nextTargetNode children
            fContinue nextPosition nextChildren

    let newTree = Node.rebuildCata next options tree    
    Configuration (newTree, toMap options newTree)

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
    
    Configuration (newTree, toMap options newTree)

/// Returns a new configuration with the section removed.
let removeSection options sectionName config =
    let (Configuration (tree, dic)) = config
    let sectionNodes = dic.GetNodes sectionName
    let newTree = List.fold (fun tree sectionNode -> Node.replace ((=) sectionNode) options [sectionNode] [] tree) tree sectionNodes
    Configuration (newTree, toMap options newTree)

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
                | SectionNode (_, (SectionHeadingNode _ as sectionHeadingNode)::_) ->
                    Node.findChildren Node.isReplaceable sectionHeadingNode

            // Replace section heading name and rebuild configuration
            let newText = [ NodeBuilder.replaceableText newName ]
            let newSection = SectionNode (newName, Node.getChildren sectionNode)
            let newSection = Node.replace Node.isReplaceable options sectionHeadingText newText newSection
            Node.replace ((=) sectionNode) options [sectionNode] [newSection] tree)

    Configuration (newTree, toMap options newTree)

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
                    | KeyNode (oldName, _, (KeyNameNode _ as keyNameNode)::_) when oldName = keyName ->
                        let keyNameText = Node.findChildren Node.isReplaceable keyNameNode
                        Node.replace Node.isReplaceable options keyNameText newText tree
                    | _ ->
                        tree))
            
    Configuration (newTree, toMap options newTree)

/// Returns a new configuration without the node.
let removeNode options targetNode (Configuration (tree, _) as config) =
    let next fContinue nextPosition children =
        match List.tryFindIndex ((=) targetNode) children with
        | None ->
            fContinue nextPosition children
        | Some targetIndex ->
            // If the target node contains a trailing newline, keep the newline
            let _, newline =
                targetNode
                |> Node.getChildren
                |> Node.splitTrailingWhitespace (Node.endsWith options "\n")
            children
            |> List.removeAt targetIndex
            |> List.insertManyAt targetIndex newline
            |> fContinue nextPosition

    let newTree = Node.rebuildCata next options tree
    Configuration (newTree, toMap options newTree)

/// Builds a new configuration from the given list of key-value pairs.
let ofList options xs = List.fold (fun config (section, key, value) -> add options section key value config) empty xs

/// Builds a new configuration from the given sequence of key-value pairs.
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

/// Reformats all keys in a tree.
let private reformatKeys options (RootNode children) =
    let convertSection (SectionNode (name, sectionChildren)) =
        let convertKey name value maybeAssignmentChar keyNode (KeyNameNode (_, keyNameChildren)) (KeyValueNode (_, keyValueChildren)) rest =
            let preferredAssignmentChar =
                match options.nameValueDelimiterPreferenceRule with
                | PreferEqualsDelimiter -> Some '='
                | PreferColonDelimiter -> Some ':'
                | PreferNoDelimiter -> None

            if preferredAssignmentChar = maybeAssignmentChar
                || options.nameValueDelimiterRule = EqualsOrColonDelimiter && maybeAssignmentChar <> None then
                let newKeyChildren =
                    keyNode
                    |> Node.getChildren
                    |> List.map (function
                        | KeyNameNode _
                        | KeyValueNode _ as node ->
                            NodeBuilder.sanitize options node
                        | node ->
                            node)
                Node.withChildren newKeyChildren keyNode
            else
                let keyNameNode =
                    let leftWhitespace =
                        match options.nameValueDelimiterSpacingRule with
                        | BothSides | LeftOnly -> [ NodeBuilder.spaceTrivia() ]
                        | _ -> []
                    let keyNameNodeStripped, _ = Node.splitTrailingWhitespace Operators.giveTrue keyNameChildren
                    let node = KeyNameNode (name, keyNameNodeStripped @ leftWhitespace)
                    if options.nameValueDelimiterRule = NoDelimiter then NodeBuilder.sanitize options node else node
                let assignmentNode =
                    match preferredAssignmentChar with
                    | Some c -> [ TokenNode (Assignment (c, 0, 0)) ]
                    | None -> []
                let keyValueNode =
                    let rightWhitespace =
                        match options.nameValueDelimiterSpacingRule with
                        | BothSides | RightOnly -> [ NodeBuilder.spaceTrivia() ]
                        | _ -> []
                    let keyValueNodeStripped, _ = Node.splitLeadingWhitespace Operators.giveTrue keyValueChildren
                    let node = KeyValueNode (value, rightWhitespace @ keyValueNodeStripped)
                    NodeBuilder.sanitize options node
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
    let newTree = reformatKeys options tree
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
