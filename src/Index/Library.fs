namespace Index
open MeCab
open System
open System.Linq
open System.IO
open DB
open Util

module Writer =
  // Create MeCabTagger in macos environment.
  let private createTagger =
    let param = new MeCabParam()
    param.DicDir <- "/usr/local/lib/mecab/dic/ipadic"
    MeCabTagger.Create param
  // Contents parse to norn surface
  let parseToNouns contents =
    contents
    |> createTagger.ParseToNodes
    |> Seq.filter (fun node -> node.CharType > 0u)
    |> Seq.filter (fun node -> node.Feature.IndexOf("名詞") = 0)
    |> Seq.map (fun node -> node.Surface)
  // Index process from single file.
  // Index data is into database.
  let indexSingleFile filepath =
    let contents = File.ReadAllText(filepath)
    let con = (fun unit -> DB.Connection.mkShared())
    Queries.Texts.insertText con filepath contents |> Async.RunSynchronously |> ignore
    let nouns = parseToNouns contents
    for noun in nouns do
      try
        DB.Queries.Morphemes.insertMorpheme con noun |> Async.RunSynchronously |> ignore
      with
        // Error shelving. evil.
        // I believe that exception other than does not Unique constraint
        // if you need debuging, this is insert it.
        // printfn "db error %s" (e.Message);
        | :? System.Exception -> ()
    let morphemes = DB.Queries.Morphemes.listMorpheme con |> Async.RunSynchronously
    let text = DB.Queries.Texts.listText con |> Async.RunSynchronously |> Seq.find (fun (x:DB.Types.Text) -> x.Filepath.Equals(filepath))
    for morp in morphemes do
      let cnt = nouns |> Seq.filter (fun x -> x.Equals(morp.Value)) |> Seq.length
      DB.Queries.TextMorphemes.insertTextMorpheme con text.Id morp.Id (int64(cnt)) |> Async.RunSynchronously
    ()
  // indexing file match the fileprn(pattern string) under the directory.
  let indexDeepDirFiles dirpath fileptn =
    let files = Directory.GetFiles(dirpath, fileptn, SearchOption.AllDirectories)
    let mutable cnt = 1
    let max = files.Length
    for file in files do
      indexSingleFile file
      printfn "indexed %s, %d/%d" file cnt max
      cnt <- cnt + 1
