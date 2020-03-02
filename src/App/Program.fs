// Learn more about F# at http://fsharp.org

open System
open Index.Writer

[<EntryPoint>]
let main argv =
  indexSingleFile argv.[0]
  0 // return an integer exit code
