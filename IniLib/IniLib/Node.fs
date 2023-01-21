namespace IniLib

open IniLib.Utilities

type Node =
    | RootNode of children: Node list
    | SectionHeadingNode of name: string * children: Node list
    | SectionNode of name: string * children: Node list
    | KeyNode of name: string * value: string * children: Node list
    | KeyNameNode of name: string * children: Node list
    | KeyValueNode of value: string * children: Node list
    | CommentNode of text: string * children: Node list
    | TriviaNode of token: Token
    | TokenNode of token: Token
    | ReplaceableTokenNode of token: Token
with
    override this.ToString () =
        match this with
        | RootNode _ -> "RootNode"
        | SectionHeadingNode (name, _) -> $"SectionHeadingNode '{name}'"
        | SectionNode (name, _) -> $"SectionNode '{name}'"
        | KeyNode (name, value, _) -> $"KeyNode ({name}, {value})"
        | KeyNameNode (name, _) -> $"KeyNameNode '{name}'"
        | KeyValueNode (value, _) -> $"KeyValueNode '{value}'"
        | CommentNode (text, _) -> $"CommentNode '{text}'"
        | TriviaNode token -> $"TriviaNode %O{token}"
        | TokenNode token -> $"TokenNode %O{token}"
        | ReplaceableTokenNode token -> $"ReplaceableTokenNode %O{token}"

    static member walk visit initialValue tree =
        let rec walk' (value: 'a) tree =
            match tree with
            | RootNode children
            | SectionHeadingNode (_, children)
            | SectionNode (_, children)
            | KeyNode (_, _, children)
            | KeyNameNode (_, children)
            | KeyValueNode (_, children)
            | CommentNode (_, children) ->
                let nextValue = visit value tree children
                List.iter (walk' nextValue) children
            | ReplaceableTokenNode _
            | TokenNode _
            | TriviaNode _ ->
                visit value tree [] |> ignore
        walk' initialValue tree

    static member inline walkCata fChildren fToken defaultValue node =
        match node with
        | RootNode children
        | SectionNode (_, children)
        | SectionHeadingNode (_, children)
        | KeyNode (_, _, children)
        | KeyNameNode (_, children)
        | KeyValueNode (_, children)
        | CommentNode (_, children) when List.length children > 0 ->
            fChildren children

        | TokenNode token
        | ReplaceableTokenNode token
        | TriviaNode token ->
            fToken token

        | _ ->
            defaultValue

    /// Gets the first text field of the node. If the node has no text field, returns "".
    static member getText1 node =
        match node with
        | SectionNode (text, _)
        | SectionHeadingNode (text, _)
        | KeyNode (text, _, _)
        | KeyNameNode (text, _)
        | KeyValueNode (text, _)
        | CommentNode (text, _) ->
            text

        | _ ->
            ""

    /// Gets the second text field of the node. If the node has no second text field, returns "".
    static member getText2 node =
        match node with
        | KeyNode (_, text, _) -> text
        | _ -> ""

    static member ofToken token =
        match token with
        | Whitespace _ -> TriviaNode token
        | _ -> TokenNode token

    static member toText options node = Node.walkCata (List.map (Node.toText options) >> String.concat "") (Token.toText options) "" node

    static member endsWith options substring = Node.toText options >> String.endsWith substring

    static member position node = Node.walkCata (List.head >> Node.position) Token.position (1, 1) node

    static member endPosition node = Node.walkCata (List.last >> Node.endPosition) Token.endPosition (1, 1) node

    static member getChildren node = Node.walkCata id (fun _ -> []) [] node

    static member hasChild predicate node = node |> Node.getChildren |> List.exists predicate

    static member startsWithChild predicate node = node |> Node.getChildren |> List.head |> predicate

    static member endsWithChild predicate node = node |> Node.getChildren |> List.last |> predicate

    static member findChildren predicate node = node |> Node.getChildren |> List.filter predicate

    static member findChild predicate node = node |> Node.getChildren |> List.find predicate

    static member tryFindChild predicate node = node |> Node.getChildren |> List.tryFind predicate

    static member findParent tree targetNode =
        let children = Node.getChildren tree
        match List.tryFind ((=) targetNode) children with
        | Some _ -> Some tree
        | None ->
            List.fold
                (fun result node ->
                    match result with
                    | Some _ -> result
                    | None -> Node.findParent node targetNode)
                None
                children
            
    static member internal setChildren fChildren node =
        match node with
        | RootNode children -> RootNode (fChildren children)
        | SectionHeadingNode (name, children) -> SectionHeadingNode (name, fChildren children)
        | SectionNode (name, children) -> SectionNode (name, fChildren children)
        | KeyNode (name, value, children) -> KeyNode (name, value, fChildren children)
        | KeyNameNode (name, children) -> KeyNameNode (name, fChildren children)
        | KeyValueNode (value, children) -> KeyValueNode (value, fChildren children)
        | CommentNode (text, children) -> CommentNode (text, fChildren children)
        | _ -> node

    static member inline internal withChildren children node = Node.setChildren (fun _ -> children) node

    static member inline internal appendChildren children node = Node.setChildren (fun oldChildren -> oldChildren @ children) node

    static member inline internal appendChild child node = Node.appendChildren [child] node

    static member inline internal prependChildren children node = Node.setChildren (List.append children) node

    static member inline internal prependChild child node = Node.prependChildren [child] node

    static member inline internal insertChildrenAt i children node = Node.setChildren (List.insertManyAt i children) node

    static member inline internal insertChildAt i child node = Node.insertChildrenAt i [child] node

    static member inline internal removeChild child node = Node.setChildren (List.filter ((<>) child)) node

    static member inline internal isWhitespace node = match node with TriviaNode (Whitespace _) -> true | _ -> false

    static member inline internal isComment node = match node with CommentNode _ -> true | _ -> false

    static member inline internal isNotComment node = match node with CommentNode _ -> false | _ -> true

    static member inline internal isReplaceable node = match node with ReplaceableTokenNode _ -> true | _ -> false

    static member inline internal isNotReplaceable node = match node with ReplaceableTokenNode _ -> false | _ -> true

    static member internal splitLeadingWhitespace (filter: Node -> bool) (nodes: Node list) =
        let leadingWhitespace = nodes |> List.takeWhile (fun n -> Node.isWhitespace n && filter n)
        let nodes = nodes[leadingWhitespace.Length..]
        nodes, leadingWhitespace

    static member internal splitTrailingWhitespace (filter: Node -> bool) (nodes: Node list) =
        let trailingWhitespace = nodes |> Seq.rev |> Seq.takeWhile (fun n -> Node.isWhitespace n && filter n) |> Seq.rev |> List.ofSeq
        let nodes = nodes[0..nodes.Length - 1 - trailingWhitespace.Length]
        nodes, trailingWhitespace

    static member copyLeadingWhitespace sourceNode targetNode =
        let _, leadingWhitespace =
            sourceNode
            |> Node.getChildren
            |> Node.splitLeadingWhitespace Operators.giveTrue
        Node.prependChildren leadingWhitespace targetNode

    static member internal joinReplaceableText options nodes =
        nodes
        |> List.choose (function
            | ReplaceableTokenNode (Comment _)
            | ReplaceableTokenNode (Text _)
            | ReplaceableTokenNode (Whitespace _) as n ->
                Some (Node.toText options n)
            | _ ->
                None)
        |> String.concat ""
        |> String.trim

    /// <summary>
    /// Visits each node in a tree, rebuilding the tree and updating the line and column of each token
    /// as it modifies each node's children using the passed function <paramref name="next"/>.
    /// </summary>
    static member internal rebuildCata fNext options tree =
        let rec rebuildCata' nextPosition tree =
            let inline stepInto position children = List.mapFold rebuildCata' position children

            match tree with
            | RootNode children ->
                let nextChildren, nextPosition = fNext stepInto nextPosition children
                RootNode nextChildren, nextPosition

            | SectionHeadingNode (_, children) ->
                let nextChildren, nextPosition = fNext stepInto nextPosition children
                SectionHeadingNode (Node.joinReplaceableText options nextChildren, nextChildren), nextPosition

            | SectionNode (_, children) ->
                let nextChildren, nextPosition = fNext stepInto nextPosition children
                let name = nextChildren |> List.tryPick (function SectionHeadingNode (name, _) -> Some name | _ -> None)
                SectionNode (Option.defaultValue "<global>" name, nextChildren), nextPosition

            | KeyNode (_, _, children) ->
                let nextChildren, nextPosition = fNext stepInto nextPosition children
                let name = nextChildren |> List.pick (function KeyNameNode (name, _) -> Some name | _ -> None)
                let value = nextChildren |> List.pick (function KeyValueNode (value, _) -> Some value | _ -> None)
                KeyNode (name, value, nextChildren), nextPosition

            | KeyNameNode (_, children) ->
                let nextChildren, nextPosition = fNext stepInto nextPosition children
                let keyName = Node.joinReplaceableText options nextChildren
                KeyNameNode (keyName, nextChildren), nextPosition

            | KeyValueNode (_, children) ->
                let nextChildren, nextPosition = fNext stepInto nextPosition children
                let keyValue = Node.joinReplaceableText options nextChildren
                KeyValueNode (keyValue, nextChildren), nextPosition

            | CommentNode (_, children) ->
                let nextChildren, nextPosition = fNext stepInto nextPosition children
                let commentText = Node.joinReplaceableText options nextChildren
                CommentNode (commentText.Trim(), nextChildren), nextPosition

            | TriviaNode token ->
                let newToken = Token.withPosition nextPosition token
                TriviaNode newToken, Token.endPosition newToken

            | TokenNode token ->
                let newToken = Token.withPosition nextPosition token
                TokenNode newToken, Token.endPosition newToken

            | ReplaceableTokenNode token ->
                let newToken = Token.withPosition nextPosition token
                ReplaceableTokenNode newToken, Token.endPosition newToken

        let newTree, _ = rebuildCata' (1, 1) tree
        newTree

    /// Updates the line and column of all tokens in a tree.
    static member internal renumber = Node.rebuildCata (fun fContinue nextPosition children -> fContinue nextPosition children)

    /// Replaces a list of target nodes with a list of replacement nodes in the tree and sets all token positions.
    static member internal replace predicate options target replacement tree =
        let next fContinue nextPosition children =
            match List.tryFindIndex predicate children, List.tryFindIndexBack predicate children with
            | Some firstReplaceableIndex, Some lastReplaceableIndex when children.[firstReplaceableIndex..lastReplaceableIndex] = target ->
                let prologue, nextPosition = fContinue nextPosition children.[0..firstReplaceableIndex - 1]
                let replacement, nextPosition = fContinue nextPosition replacement
                let epilogue, nextPosition = fContinue nextPosition children.[lastReplaceableIndex + 1..]
                let children = prologue @ replacement @ epilogue
                children, nextPosition

            | _ ->
                fContinue nextPosition children

        Node.rebuildCata next options tree
