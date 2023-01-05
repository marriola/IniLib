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

module Node =

    let walk<'a> visit (initialValue: 'a) tree =
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

    let inline private nodeCata fChildren fToken defaultValue = function
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

    let rec toText options node = nodeCata (List.map (toText options) >> String.concat "") (Token.toText options) "" node

    let rec position node = nodeCata (List.head >> position) Token.position (1, 1) node

    let rec endPosition node = nodeCata (List.last >> endPosition) Token.endPosition (1, 1) node

    let getChildren = nodeCata id (fun _ -> []) []

    let addChild child node =
        match node with
        | RootNode children -> RootNode (children @ [child])
        | SectionHeadingNode (name, children) -> SectionHeadingNode (name, children @ [child])
        | SectionNode (name, children) -> SectionNode (name, children @ [child])
        | KeyNode (name, value, children) -> KeyNode (name, value, children @ [child])
        | KeyNameNode (name, children) -> KeyNameNode (name, children @ [child])
        | KeyValueNode (value, children) -> KeyValueNode (value, children @ [child])
        | CommentNode (text, children) -> CommentNode (text, children @ [child])
        | _ -> node

    let internal insertNewlinesIfNeeded options nodes =
        let newlineText = options.newlineRule.toText()

        let rec insertNewlinesIfNeeded' output nodes =
            match nodes with
            | [] -> List.rev output
            | n::rest when not (n |> toText options |> String.endsWith "\n") ->
                let newline = TriviaNode (Whitespace (newlineText, 0, 0))
                insertNewlinesIfNeeded' (newline :: n :: output) rest
            | n::rest ->
                insertNewlinesIfNeeded' (n :: output) rest
        
        insertNewlinesIfNeeded' [] nodes

    let inline internal isWhitespace node = match node with TriviaNode (Whitespace _) -> true | _ -> false
    let inline internal isReplaceable node = match node with ReplaceableTokenNode _ -> true | _ -> false
    let internal isNotReplaceable = (isReplaceable >> not)

    /// <summary>
    /// Replaces a list of target nodes with a list of replacement nodes in the tree and sets all token positions.
    /// </summary>
    let internal replace predicate options target replacement tree =
        let joinReplaceableNodeText nodes =
            nodes
            |> List.choose (function ReplaceableTokenNode (Text _) | ReplaceableTokenNode (Whitespace _) as n -> Some n | _ -> None)
            |> List.map (toText options)
            |> String.concat ""

        let rec replace' nextPosition tree =
            let next children =
                match List.tryFindIndex predicate children, List.tryFindIndexBack predicate children with
                | Some firstReplaceableIndex, Some lastReplaceableIndex when children.[firstReplaceableIndex..lastReplaceableIndex] = target ->
                    let prologue, nextPosition = List.mapFold replace' nextPosition children.[0..firstReplaceableIndex - 1]
                    let replacement, nextPosition = List.mapFold replace' nextPosition replacement
                    let epilogue, nextPosition = List.mapFold replace' nextPosition children.[lastReplaceableIndex + 1..]
                    let children = prologue @ replacement @ epilogue
                    children, nextPosition

                | _ ->
                    List.mapFold replace' nextPosition children

            match tree with
            | RootNode children ->
                let nextChildren, nextPosition = next children
                RootNode nextChildren, nextPosition

            | SectionHeadingNode (_, children) ->
                let nextChildren, nextPosition = next children
                SectionHeadingNode (joinReplaceableNodeText nextChildren, nextChildren), nextPosition

            | SectionNode (name, children) ->
                let nextChildren, nextPosition = next children
                SectionNode (name, nextChildren), nextPosition

            | KeyNode (_, _, children) ->
                let nextChildren, nextPosition = next children
                let name = nextChildren |> List.pick (function KeyNameNode (name, _) -> Some name | _ -> None)
                let value = nextChildren |> List.pick (function KeyValueNode (value, _) -> Some value | _ -> None)
                KeyNode (name, value, nextChildren), nextPosition

            | KeyNameNode (_, children) ->
                let nextChildren, nextPosition = next children
                KeyNameNode (joinReplaceableNodeText nextChildren, nextChildren), nextPosition

            | KeyValueNode (_, children) ->
                let nextChildren, nextPosition = next children
                KeyValueNode (joinReplaceableNodeText nextChildren, nextChildren), nextPosition

            | CommentNode (text, children) ->
                let nextChildren, nextPosition = next children
                CommentNode (text, nextChildren), nextPosition

            | TriviaNode token ->
                let newToken = Token.withPosition nextPosition token
                TriviaNode newToken, Token.endPosition newToken

            | TokenNode token ->
                let newToken = Token.withPosition nextPosition token
                TokenNode newToken, Token.endPosition newToken

            | ReplaceableTokenNode token ->
                let newToken = Token.withPosition nextPosition token
                ReplaceableTokenNode newToken, Token.endPosition newToken

        let newTree, _ = replace' (1, 1) tree
        newTree
