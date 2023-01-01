namespace IniLib

open System.Collections.Generic

module Configuration =

    open FSharpPlus
    open IniLib
    open System
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

    let private isNodeReplaceable = function ReplaceableTokenNode _ -> true | _ -> false

    let private toMap options (RootNode children) =
        let getKeys nodes =
            let rec getKeys' addedKeys out nodes =
                match nodes with
                | [] ->
                    out
                    |> List.rev
                    |> List.groupBy fst
                    |> List.map (fun ((key, value) as x) -> key, List.map snd value)
                    |> Map.ofList
                    |> KeyMap

                | KeyNode (name, value, _) as keyNode::rest ->
                    let newEntry = (name, (value, keyNode))

                    let nextOut =
                        match options.duplicateKeyRule with
                        | DuplicateKeyReplacesValue
                        | DisallowDuplicateKeys when Set.contains name addedKeys ->
                            List.map (fun ((n, _) as oldEntry) -> if n = name then newEntry else oldEntry) out
                        | _ ->
                            newEntry :: out

                    getKeys' (Set.add name addedKeys) nextOut rest

                | _::rest ->
                    getKeys' addedKeys out rest

            getKeys' Set.empty [] nodes

        let rec getSections' out nodes =
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
                        out |> List.map (fun ((n, pair) as entry) ->
                            if n = name then
                                let keys1, nodes1 = pair
                                let keys2 = getKeys children
                                (name, (keys1 + keys2, sectionNode :: nodes1))
                            else
                                entry)
                    else
                        (name, (getKeys children, [ sectionNode ])) :: out
                getSections' nextOut rest

            | _::rest ->
                getSections' out rest

        getSections' [] children

    /// Parses a configuration from text.
    let fromText options text =
        let tree =
            text
            |> Lexer.lex options
            |> Parser.parse options

        Configuration (tree, toMap options tree)

    /// Parses a configuration from a stream.
    let fromStream options (stream: Stream) =
        let reader = new StreamReader(stream)
        fromText options (reader.ReadToEnd())

    /// Parses a configuration from a stream reader.
    let fromStreamReader options (reader: StreamReader) =
        fromText options (reader.ReadToEnd())

    /// Parses a configuration from a file.
    let fromFile options path =
        path
        |> File.ReadAllText
        |> fromText options

    /// Converts all assignment tokens and the surrounding spacing if they disagree with the applied name value delimiter preference and spacing rules
    let private convertNameValueDelimiters options (RootNode children) =
        let convertSection (SectionNode (name, sectionChildren)) =
            let convertKey name value maybeAssignmentChar keyNode (KeyNameNode (_, keyNameChildren)) (KeyValueNode (_, keyValueChildren)) rest =
                let preferredAssignmentChar =
                    match options.nameValueDelimiterPreferenceRule with
                    | PreferEqualsDelimiter -> Some '='
                    | PreferColonDelimiter -> Some ':'
                    | PreferNoDelimiter -> None
                if preferredAssignmentChar = maybeAssignmentChar then
                    keyNode
                else
                    let leftWhitespace =
                        match options.nameValueDelimiterSpacingRule with
                        | BothSides
                        | LeftOnly -> [ TriviaNode (Whitespace (" ", 0, 0)) ]
                        | _ -> []
                    let rightWhitespace =
                        match options.nameValueDelimiterSpacingRule with
                        | BothSides
                        | RightOnly -> [ TriviaNode (Whitespace (" ", 0, 0)) ]
                        | _ -> []
                    let keyNameNodeStripped =
                        keyNameChildren
                        |> List.rev
                        |> List.skipWhile (function TriviaNode (Whitespace _) -> true | _ -> false)
                        |> List.rev
                    let keyValueNodeStripped =
                        keyValueChildren
                        |> List.skipWhile (function TriviaNode (Whitespace _) -> true | _ -> false)
                    let assignmentNode =
                        match preferredAssignmentChar with
                        | Some c -> [ TokenNode (Assignment (c, 0, 0)) ]
                        | None -> []
                    let keyNameNode = KeyNameNode (name, keyNameNodeStripped @ leftWhitespace)
                    let keyValueNode = KeyValueNode (value, rightWhitespace @ keyValueNodeStripped)
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

    /// Writes a configuration to a file.
    let writeToFile options (path: string) config =
        let streamWriter = new StreamWriter(path)
        streamWriter.Write(toText options config)

    /// Writes a configuration to a stream writer.
    let writeToStreamWriter options (streamWriter: StreamWriter) config =
        streamWriter.Write(toText options config)

    /// Writes a configuration to a stream.
    let writeToStream options (encoding: System.Text.Encoding) (stream: Stream) config =
        let text = toText options config
        let buffer = encoding.GetBytes(text)
        stream.Write(buffer, 0, buffer.Length)

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
        let newlineText = options.newlineRule.toText()

        let getKeyNodeChildren = function
            | KeyNode (_, _, KeyNameNode _ :: TokenNode (Assignment _) :: KeyValueNode (_, children) :: _)
            | KeyNode (_, _, KeyNameNode _ :: KeyValueNode (_, children) :: _) ->
                children

        let replace sectionName keyName value =
            // Single key already exists, replace value and existing text nodes
            let section = dic.[sectionName]
            let keyNode::_ = section.GetNodes(keyName)
            let target = List.filter isNodeReplaceable (getKeyNodeChildren keyNode)
            let line, column = Node.position target.[0]

            // Create new token node and replace original nodes
            let newText = NodeBuilder.keyValueText options value
            let newTree = Node.replace isNodeReplaceable options target newText tree
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
                | _ -> None

            let lastKeyIndex =
                lastMatchingKeyIndex
                |> Option.orElseWith (fun () -> List.tryFindIndexBack (function KeyNode _ -> true | _ -> false) children)
                |> Option.defaultValue -1

            let lastKeyText = if lastKeyIndex = -1 then "" else Node.toText options children.[lastKeyIndex]

            // Insert a newline if the last key didn't end in a newline
            let newline =
                if lastKeyText.EndsWith("\n") then
                    []
                else
                    [ TriviaNode (Whitespace (newlineText, 0, 0)) ]

            // Copy leading whitespace from last key
            let keyNode =
                let regexMatch = RE_LEADING_WHITESPACE.Match(lastKeyText)
                if regexMatch.Success then
                    let (KeyNode (name, value, KeyNameNode (_, newKeyNameNodes)::rest)) = keyNode
                    let whitespaceNode = TriviaNode (Whitespace (regexMatch.Groups[0].Value, 0, 0))
                    let keyNameNode = KeyNameNode (name, [whitespaceNode] @ newKeyNameNodes)
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
            let lastChildText =
                children
                |> List.tryLast
                |> Option.map (Node.toText options)
                |> Option.defaultValue "\n"
            let newline =
                if lastChildText.EndsWith("\n")
                    then None
                    else Some (TriviaNode (Whitespace (newlineText, 0, 0)))
            let nodesOut = [
                SectionNode (sectionName, [
                    SectionHeadingNode (sectionName, [
                        TokenNode (LeftBracket (0, 0))
                        ReplaceableTokenNode (Text (sectionName, 0, 0))
                        TokenNode (RightBracket (0, 0))
                        TokenNode (Whitespace (newlineText, 0, 0))
                    ])
                    keyNode
                ])
                TriviaNode (Whitespace (newlineText, 0, 0))
            ]

            let newTree = RootNode (children @ Option.toList newline @ nodesOut)
            let newDic = toMap options newTree

            Configuration (newTree, newDic)

        let section =
            dic
            |> SectionMap.unwrap
            |> Map.tryFind sectionName
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
                        List.filter isNodeReplaceable children

                let (SectionNode (_, sectionChildren)) = sectionNode

                // Replace section heading name and rebuild configuration
                let newText = [ReplaceableTokenNode (Text (newName, 0, 0))]
                let newSection = SectionNode (newName, sectionChildren)
                let newSection = Node.replace isNodeReplaceable options sectionHeadingText newText newSection
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
                let newText = [ReplaceableTokenNode (Text (newKeyName, 0, 0))]

                // Replace name in matching keys
                (tree, children)
                ||> List.fold
                    (fun tree node ->
                        match node with
                        | KeyNode (oldName, _, KeyNameNode (_, children)::_) when oldName = keyName ->
                            let keyNameText = List.filter isNodeReplaceable children
                            Node.replace isNodeReplaceable options keyNameText newText tree
                        | _ ->
                            tree))
            
        Configuration (newTree, toMap options newTree)
