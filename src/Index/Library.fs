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
    // |> Seq.filter (fun node -> node.Feature.IndexOf("名詞") = 0)
    |> Seq.map (fun node -> node.Surface)
  // Index process from single file.
  // Index data is into database.
  let indexSingleFile filepath =
    async {
      printfn "%s" filepath
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
      let texts = DB.Queries.Texts.listText con |> Async.RunSynchronously
      let text = texts |> Seq.find (fun (x:DB.Types.Text) -> x.Filepath.Equals(filepath))
      for morp in morphemes do
        let cnt = nouns |> Seq.filter (fun x -> x.Equals(morp.Value)) |> Seq.length
        let tf = (double cnt) / (double (nouns.Count()))
        DB.Queries.TextMorphemes.insertTextMorpheme con text.Id morp.Id 0.0 tf 0.0 |> Async.RunSynchronously
      ()
    }
  let scoreUpdate =
    printfn "calc score..."
    let con = (fun unit -> DB.Connection.mkShared())
    let morphemes = DB.Queries.Morphemes.listMorpheme con |> Async.RunSynchronously
    let texts = DB.Queries.Texts.listText con |> Async.RunSynchronously
    let tmLst = DB.Queries.TextMorphemes.listTextMorpheme con |> Async.RunSynchronously
    for tm in tmLst do
      for m in morphemes do
        // printfn "tm-mid: %d, m-id: %d" tm.MorphemeId m.Id
        match tm.MorphemeId = m.Id with
          | false -> None
          | true ->
            for t in texts do
              match t.Id = tm.TextId with
                | false -> None
                | true ->
                  let tf = tm.Tf
                  let textIncMorpCnt = (tmLst |> Seq.filter (fun x -> x.MorphemeId = m.Id)) |> Seq.groupBy (fun x -> x.TextId) |> Seq.length
                  let idf = Math.Log((float (texts.Count())) / (float textIncMorpCnt))
                  let score = tf * idf
                  // printfn "idf:%f, tf-idf:%f" idf score
                  Queries.TextMorphemes.updateTextMorpheme con tm.TextId tm.MorphemeId score tf idf |> Async.RunSynchronously |> ignore
                  None
            None
    printfn "calc score..."
  // indexing file match the fileprn(pattern string) under the directory.
  let indexDeepDirFiles dirpath fileptn =
    let con = (fun unit -> DB.Connection.mkShared())
    let texts = DB.Queries.Texts.listText con |> Async.RunSynchronously
    let files = Directory.GetFiles(dirpath, fileptn, SearchOption.AllDirectories) |> Seq.filter (fun x -> (texts |> Seq.tryFind (fun y -> y.Filepath.Equals(x))).IsNone)
    let max = files.Count()
    files.ToArray() |> Array.map indexSingleFile |> Async.Parallel |> Async.Ignore |> Async.RunSynchronously
