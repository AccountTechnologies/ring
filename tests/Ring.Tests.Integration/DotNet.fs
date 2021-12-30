namespace Ring.Test.Integration.DotNet

open Fake.Core

module Types =

  type Options = {
    NuGetName: string
    NuGetSourcePath: string
    NuGetConfigPath: string
    SrcPath: string
    PackageVersion: string
    LocalTool: LocalTool option
    WorkingDir: string
  }
  and LocalTool = {
    InstallPath: string
    ManifestFilePath: string
  }

module Dotnet =
  open Types
  
  let createProc name workingDir args =
    Process.setEnableProcessTracing true
    RawCommand(name,
    Arguments.OfArgs args)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode

  let proc name workingDir args =
    createProc name workingDir args |> Proc.start

  let procWithResult name workingDir args =
    task {
      let! result =
        createProc name workingDir args
        |> CreateProcess.redirectOutput
        |> Proc.start
      return result.Result.Output.TrimEnd()
    }

  let newToolManifest (workingDir:string) = task {
    return! (proc "dotnet" workingDir [ "new"; "tool-manifest"])
   }

  let installTool (tool:Options) = task {

    match tool.LocalTool with
    | None -> ()
    | Some manifest ->
      let! _ = (manifest.InstallPath |> newToolManifest)
      ()

    return!
      proc "dotnet" tool.WorkingDir [
        "tool"; "install"; tool.NuGetName
        "--version"; tool.PackageVersion
        "--add-source"; tool.NuGetSourcePath
        "--configfile"; tool.NuGetConfigPath
        "--no-cache"
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
      proc "dotnet" tool.WorkingDir [
        "tool"; "uninstall"; tool.NuGetName
        match tool.LocalTool with
        | None _ ->
          "--global"
        | Some (manifest) ->
          "--tool-manifest"
          manifest.ManifestFilePath
      ]
  }
