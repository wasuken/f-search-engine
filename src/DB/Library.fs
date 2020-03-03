namespace DB

open FSharp.Data.Dapper
open Microsoft.Data.Sqlite
open Util.Text
open System

module Connection =
    let private mkConnectionString (dataSource : string) =
        sprintf
            "Data Source = %s%s;"
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config/f-search-engine/")
            dataSource
    let mkShared () =
      Connection.SqliteConnection (new SqliteConnection (mkConnectionString "db.sqlite"))

module Types =
    [<CLIMutable>]
    type Morpheme = {
      Id: int64
      Value: string
    }
    [<CLIMutable>]
    type Text = {
      Id: int64
      Contents: string
      Filepath: string
      ContentsHash: string
    }
    [<CLIMutable>]
    type TextMorpheme = {
      TextId: int64
      MorphemeId: int64
      Tf: Double
      Idf: Double
      Score: Double
    }

module Queries =
    module Texts =
      let insertText con (filepath:string) (contents:string) =
        let hash = simpleSha1Str (filepath + contents)
        let querySingleOptionAsync:QuerySingleOptionAsyncBuilder<Types.Text> =
          (querySingleOptionAsync<Types.Text> con)
        querySingleOptionAsync {
          script "insert into texts(filepath, contents, contents_hash) values(@Filepath, @Contents, @ContentsHash)"
          parameters (dict ["Filepath", box filepath; "Contents", box contents; "ContentsHash", box hash])
        }
      let listText con =
        let querySeqAsync:QuerySeqAsyncBuilder<Types.Text> =
          (querySeqAsync<Types.Text> con)
        querySeqAsync {
          script "select id, filepath, contents, contents_hash as contentsHash from texts"
        }
    module Morphemes =
      let insertMorpheme con value =
        let querySingleOptionAsync:QuerySingleOptionAsyncBuilder<Types.Morpheme> =
          (querySingleOptionAsync<Types.Morpheme> con)
        querySingleOptionAsync {
          script "insert into morphemes(value) values(@Value)"
          parameters (dict ["Value", box value])
        }
      let listMorpheme con =
        let querySeqAsync:QuerySeqAsyncBuilder<Types.Morpheme> =
          (querySeqAsync<Types.Morpheme> con)
        querySeqAsync {
          script "select * from morphemes"
        }
    module TextMorphemes =
      let insertTextMorpheme con tid mid score tf idf =
        let querySingleOptionAsync:QuerySingleOptionAsyncBuilder<Types.TextMorpheme> =
          (querySingleOptionAsync<Types.TextMorpheme> con)
        querySingleOptionAsync {
          script "insert into text_morphemes(text_id, morpheme_id, score, tf, idf) values(@TextId, @MorphemeId, @Score, @Tf, @Idf)"
          parameters (dict ["TextId", box tid; "MorphemeId", box mid; "Score", box score; "Tf", box tf; "Idf", box idf])
        }
      let updateTextMorpheme con tid mid score tf idf =
        let querySingleOptionAsync:QuerySingleOptionAsyncBuilder<Types.TextMorpheme> =
          (querySingleOptionAsync<Types.TextMorpheme> con)
        querySingleOptionAsync {
          script "update text_morphemes set text_id = @TextId, morpheme_id = @MorphemeId, score = @Score, tf = @Tf, idf = @Idf where text_id = @TextId and morpheme_id = @MorphemeId"
          parameters (dict ["TextId", box tid; "MorphemeId", box mid; "Score", box score; "Tf", box tf; "Idf", box idf])
        }
      let listTextMorpheme con =
        let querySeqAsync:QuerySeqAsyncBuilder<Types.TextMorpheme> =
          (querySeqAsync<Types.TextMorpheme> con)
        querySeqAsync {
          script "select text_id as textId, morpheme_id as morphemeId, score, tf, idf from text_morphemes"
        }
