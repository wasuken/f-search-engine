namespace Index
open MeCab
open System
open System.IO
open DB
open Util

module Writer =
  let private createTagger =
    let param = new MeCabParam()
    param.DicDir <- "/usr/local/lib/mecab/dic/ipadic"
    MeCabTagger.Create param
  let parseToNouns contents =
    contents
    |> createTagger.ParseToNodes
    |> Seq.filter (fun node -> node.CharType > 0u)
    |> Seq.filter (fun node -> node.Feature.IndexOf("名詞") = 0)
    |> Seq.map (fun node -> node.Surface)
  let indexSingleFile filepath =
    let contents = File.ReadAllText(filepath)
    let con = (fun unit -> DB.Connection.mkShared())
    Queries.Texts.insertText con filepath contents |> Async.RunSynchronously
    let nouns = parseToNouns contents
    for noun in nouns do
      try
        DB.Queries.Morphemes.insertMorpheme con noun |> Async.RunSynchronously
      with
        // 普通に使っててもエラー祭りはやばいのであとで直す。
        | :? System.Exception as e -> printfn "db error %s" (e.Message); None
    let morphemes = DB.Queries.Morphemes.listMorpheme con |> Async.RunSynchronously
    let text = DB.Queries.Texts.listText con |> Async.RunSynchronously |> Seq.find (fun (x:DB.Types.Text) -> x.ContentsHash.Equals(Util.Text.simpleSha1Str (filepath + contents)))
    for morp in morphemes do
      let cnt = nouns |> Seq.filter (fun x -> x.Equals(morp.Value)) |> Seq.length
      DB.Queries.TextMorphemes.insertTextMorpheme con text.Id morp.Id (int64(cnt)) |> Async.RunSynchronously
    ()
