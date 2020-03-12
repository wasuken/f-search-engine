// Learn more about F# at http://fsharp.org

open System
open Index.Writer
open System.Linq

let threeArg act second third =
  match act with
    | "index" -> indexDeepDirFiles second third
    | _ -> printfn "failed command."

let twoArg act second =
  match act with
    | "index" -> indexSingleFile second |> Async.RunSynchronously |> ignore
    | _ -> printfn "failed command."


[<EntryPoint>]
let main argv =
  match argv.Length with
    | 3 -> threeArg (argv.First()) argv.[1] argv.[2]
    | 2 -> twoArg (argv.First()) argv.[1]
    | _ -> printfn "failed length command"
  scoreUpdate
  0 // return an integer exit code
