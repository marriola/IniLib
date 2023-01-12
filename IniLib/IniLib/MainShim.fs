// The presence of an entry point in this assembly will cause static module initializers to run when referenced from another assembly

module MainShim

#if FABLE_COMPILER
()
#else
[<EntryPoint>]
let main _ =
    0
#endif
