namespace Util

open System.Security.Cryptography
open System.IO
open System

module Text =
  let simpleSha1Str contents =
    let contentsBytes = System.Text.Encoding.UTF8.GetBytes((Seq.toArray contents))
    let bytes = contentsBytes |> HashAlgorithm.Create("SHA1").ComputeHash
    BitConverter.ToString(bytes).Replace("-", "").ToLower()
