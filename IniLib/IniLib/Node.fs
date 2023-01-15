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

    static member childrenCata fChildren fToken node =
        match node with
        | RootNode children
        | SectionHeadingNode (_, children)
        | SectionNode (_, children)
        | KeyNode (_, _, children)
        | KeyNameNode (_, children)
        | KeyValueNode (_, children)
        | CommentNode (_, children) ->
            fChildren node

        | TokenNode token
        | ReplaceableTokenNode token
        | TriviaNode token ->
            fToken node

    static member inline walkCata fChildren fToken defaultValue node =
        match node with
        | RootNode children
        | SectionHeadingNode (_, children)
        | SectionNode (_, children)
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

    static member inline fold fChildren fToken value node =
        match node with
        | RootNode children
        | SectionHeadingNode (_, children)
        | SectionNode (_, children)
        | KeyNode (_, _, children)
        | KeyNameNode (_, children)
        | KeyValueNode (_, children)
        | CommentNode (_, children) when List.length children > 0 ->
            fChildren value node children

        | TokenNode token
        | ReplaceableTokenNode token
        | TriviaNode token ->
            fToken value node token

        | _ ->
            value

    static member ofToken token =
        match token with
        | Whitespace _ -> TriviaNode token
        | _ -> TokenNode token

    static member toText options node = Node.walkCata (List.map (Node.toText options) >> String.concat "") (Token.toText options) "" node

    static member position node = Node.walkCata (List.head >> Node.position) Token.position (1, 1) node

    static member endPosition node = Node.walkCata (List.last >> Node.endPosition) Token.endPosition (1, 1) node

    static member getChildren node = Node.walkCata id (fun _ -> []) [] node

    static member withChildren children node =
        match node with
        | RootNode _ -> RootNode children
        | SectionHeadingNode (name, _) -> SectionHeadingNode (name, children)
        | SectionNode (name, _) -> SectionNode (name, children)
        | KeyNode (name, value, _) -> KeyNode (name, value, children)
        | KeyNameNode (name, _) -> KeyNameNode (name, children)
        | KeyValueNode (value, _) -> KeyValueNode (value, children)
        | CommentNode (text, _) -> CommentNode (text, children)
        | _ -> node

    static member addChildren children2 node =
        match node with
        | RootNode children -> RootNode (children @ children2)
        | SectionHeadingNode (name, children) -> SectionHeadingNode (name, children @ children2)
        | SectionNode (name, children) -> SectionNode (name, children @ children2)
        | KeyNode (name, value, children) -> KeyNode (name, value, children @ children2)
        | KeyNameNode (name, children) -> KeyNameNode (name, children @ children2)
        | KeyValueNode (value, children) -> KeyValueNode (value, children @ children2)
        | CommentNode (text, children) -> CommentNode (text, children @ children2)
        | _ -> node

    static member addChild child node = Node.addChildren [child] node

    static member removeChild child node =
        let filter children = List.filter ((<>) child) children
        match node with
        | RootNode children -> RootNode (filter children)
        | SectionHeadingNode (name, children) -> SectionHeadingNode (name, filter children)
        | SectionNode (name, children) -> SectionNode (name, filter children)
        | KeyNode (name, value, children) -> KeyNode (name, value, filter children)
        | KeyNameNode (name, children) -> KeyNameNode (name, filter children)
        | KeyValueNode (value, children) -> KeyValueNode (value, filter children)
        | CommentNode (text, children) -> CommentNode (text, filter children)
        | _ -> node

    static member addChildToBeginning node child =
        match node with
        | RootNode children -> RootNode ([child] @ children)
        | SectionHeadingNode (name, children) -> SectionHeadingNode (name, [child] @ children)
        | SectionNode (name, children) -> SectionNode (name, [child] @ children)
        | KeyNode (name, value, children) -> KeyNode (name, value, [child] @ children)
        | KeyNameNode (name, children) -> KeyNameNode (name, [child] @ children)
        | KeyValueNode (value, children) -> KeyValueNode (value, [child] @ children)
        | CommentNode (text, children) -> CommentNode (text, [child] @ children)
        | _ -> node

    static member inline internal isWhitespace node = match node with TriviaNode (Whitespace _) -> true | _ -> false
    static member inline internal isComment node = match node with CommentNode _ -> true | _ -> false
    static member inline internal isNotComment node = match node with CommentNode _ -> false | _ -> true
    static member inline internal isReplaceable node = match node with ReplaceableTokenNode _ -> true | _ -> false
    static member inline internal isNotReplaceable node = match node with ReplaceableTokenNode _ -> false | _ -> true

    static member splitTrailingWhitespace (filter: Node -> bool) (nodes: Node list) =
        let trailingWhitespace = nodes |> Seq.rev |> Seq.takeWhile (fun n -> Node.isWhitespace n && filter n) |> Seq.rev |> List.ofSeq
        let nodes = nodes[0..nodes.Length - 1 - trailingWhitespace.Length]
        nodes, trailingWhitespace

    static member takeTrailingNewline node =
        match node with
        | _ -> ()

    static member internal joinReplaceableText options nodes =
        nodes
        |> List.choose (function ReplaceableTokenNode (Comment _) | ReplaceableTokenNode (Text _) | ReplaceableTokenNode (Whitespace _) as n -> Some n | _ -> None)
        |> List.map (Node.toText options)
        |> String.concat ""

    /// <summary>
    /// Visits each node in a tree, rebuilding the tree and updating the line and column of each token
    /// as it modifies each node's children using the passed function <paramref name="next"/>.
    /// </summary>
    static member internal rebuildCata next options tree =
        let rec rebuildCata' nextPosition tree =
            let inline stepInto position children = List.mapFold rebuildCata' position children

            match tree with
            | RootNode children ->
                let nextChildren, nextPosition = next stepInto nextPosition children
                RootNode nextChildren, nextPosition

            | SectionHeadingNode (_, children) ->
                let nextChildren, nextPosition = next stepInto nextPosition children
                SectionHeadingNode (Node.joinReplaceableText options nextChildren, nextChildren), nextPosition

            | SectionNode (_, children) ->
                let nextChildren, nextPosition = next stepInto nextPosition children
                let name = nextChildren |> List.tryPick (function SectionHeadingNode (name, _) -> Some name | _ -> None)
                SectionNode (Option.defaultValue "<global>" name, nextChildren), nextPosition

            | KeyNode (_, _, children) ->
                let nextChildren, nextPosition = next stepInto nextPosition children
                let name = nextChildren |> List.pick (function KeyNameNode (name, _) -> Some name | _ -> None)
                let value = nextChildren |> List.pick (function KeyValueNode (value, _) -> Some value | _ -> None)
                KeyNode (name, value, nextChildren), nextPosition

            | KeyNameNode (_, children) ->
                let nextChildren, nextPosition = next stepInto nextPosition children
                KeyNameNode (Node.joinReplaceableText options nextChildren, nextChildren), nextPosition

            | KeyValueNode (_, children) ->
                let nextChildren, nextPosition = next stepInto nextPosition children
                KeyValueNode (Node.joinReplaceableText options nextChildren, nextChildren), nextPosition

            | CommentNode (_, children) ->
                let nextChildren, nextPosition = next stepInto nextPosition children
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

    /// <summary>
    /// Replaces a list of target nodes with a list of replacement nodes in the tree and sets all token positions.
    /// </summary>
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
