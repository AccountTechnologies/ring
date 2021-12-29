namespace Ring.Test.Integration.Install

open Fake.Core

type Options = {
  NuGetName: string
  NuGetSourcePath: string
  NuGetConfigPath: string
  SrcPath: string
  PackageVersion: string
  LocalTool: LocalTool option
}
and LocalTool = {
  InstallPath: string
  ManifestFilePath: string
}


module Dotnet =

  let createProc name args =
    RawCommand(name,
    Arguments.OfArgs args)
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCode

  let dotnet args =
    task {
      return! (createProc "dotnet" args |> Proc.start)
    }

  let procWithResult name args =
    task {
      let! result =
        createProc name args
        |> CreateProcess.redirectOutput
        |> Proc.start
      return result.Result.Output.TrimEnd()
    }

  let newToolManifest (outDir:string) =
    dotnet [
      "new"; "tool-manifest"; "--output"; outDir
    ]

  let installTool (tool:Options) = task {

    match tool.LocalTool with
    | None -> ()
    | Some manifest ->
      let! _ = (manifest.InstallPath |> newToolManifest)
      ()

    return!
      dotnet [
        "tool"; "install"; tool.NuGetName
        "--version"; tool.PackageVersion
        "--add-source"; tool.NuGetSourcePath
        "--configfile"; tool.NuGetConfigPath
        match tool.LocalTool with
        | None _ ->
          "--global"
        | Some (manifest) ->
          "--tool-manifest"
          manifest.ManifestFilePath
      ]
  }

  let uninstallTool (tool:Options) = task {

    return!
      dotnet [
        "tool"; "uninstall"; tool.NuGetName
        match tool.LocalTool with
        | None _ ->
          "--global"
        | Some (manifest) ->
          "--tool-manifest"
          manifest.ManifestFilePath
      ]
  }

module Tool =

  type Ring(options: Options) =
    let exec (args:string list) =
      match options.LocalTool with
      | None -> Dotnet.procWithResult "ring" args
      | Some _ -> Dotnet.procWithResult "dotnet" ("ring"::args)
    member _.Install() = task {
      let! _ = Dotnet.installTool options
      ()
    }
    member _.Uninstall() = task {
      let! _ = Dotnet.uninstallTool options
      ()
    }
    member _.Options = options
    member _.ShowConfig() =
      task {
        return! exec ["show-config"]
      }
