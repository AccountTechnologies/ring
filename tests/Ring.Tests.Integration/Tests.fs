module Tests

open Expecto
open Fake.Core
open Ring.Test.Integration.Install
open Ring.Test.Integration.Install.Tool
open System.IO
open System
open System.Threading.Tasks

let ciBuild = not <| BuildServer.isLocalBuild
let srcPath = Environment.environVarOrDefault "SRC_PATH" "src/ATech.Ring.DotNet.Cli"
let env name = Environment.environVar name

type TestDir() =
  let origDir = Directory.GetCurrentDirectory ()
  let dir =
    let d = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) |> Directory.CreateDirectory
    Directory.SetCurrentDirectory d.FullName
    d
  member _.WorkPath = dir.FullName
  member _.InSourceDir (path) = Path.Combine(origDir, path) |> Path.GetFullPath
  interface IDisposable with
    member _.Dispose(): unit =
      Directory.SetCurrentDirectory origDir
      dir.Delete(true)

let localOptions (dir:TestDir) = {
  SrcPath = srcPath
  PackageVersion = Environment.environVarOrDefault "PKGVER" "0.0.0-dev"
  NuGetSourcePath = Environment.environVarOrDefault "BUILD_ARTIFACTSTAGINGDIRECTORY" (dir.InSourceDir "../../src/ATech.Ring.DotNet.Cli/bin/Release")
  NuGetName = "atech.ring.dotnet.cli"
  NuGetConfigPath = dir.InSourceDir "../resources/NuGet.config"
  LocalTool = Some {
    InstallPath = dir.WorkPath
    ManifestFilePath = ".config/dotnet-tools.json"
  }
}

let globalOptions (dir:TestDir) = { localOptions dir with LocalTool = None}

let withContext (opts: TestDir -> Options) (func: Tool.Ring -> TestDir -> Task<'a>) =
  task {
    use dir = new TestDir()
    let ring = Ring(dir |> opts)
    do! ring.Install()
    let! result = func ring dir
    do! ring.Uninstall()
    return (result, ring.Options)
  }

let asLocalTool func = withContext localOptions func
let asGlobalTool func = withContext globalOptions  func

[<Tests>]
let tests =
  testList "Local tool smoke tests" [

    testTask "show-config" {
      let! acutalPath, options = asLocalTool <| fun ring _ -> task { return! ring.ShowConfig() }
      let pkgVer = options.PackageVersion
      let expectedPath =
        if Environment.isWindows
        then $"""{env "USERPROFILE"}\.nuget\packages\atech.ring.dotnet.cli\{pkgVer}\tools\net6.0\any\appsettings.json"""
        else $"""{env "HOME"}/.nuget/packages/atech.ring.dotnet.cli/{pkgVer}/tools/net6.0/any/appsettings.json"""

      "Config path should be correct" |> Expect.equal acutalPath expectedPath
    }
  ]

[<Tests>]
let globalTests =
  testList "Global tool smoke tests" [
    testTask "show-config" {
      let! acutalPath, options = asGlobalTool <| fun ring _ -> task { return! ring.ShowConfig() }
      let pkgVer = options.PackageVersion
      let expectedPath =
        if Environment.isWindows
        then $"""{env "USERPROFILE"}\.dotnet\tools\.store\atech.ring.dotnet.cli\{pkgVer}\atech.ring.dotnet.cli\{pkgVer}\tools\net6.0\any\appsettings.json"""
        else $"""{env "HOME"}/.dotnet/tools/.store/atech.ring.dotnet.cli/{pkgVer}/atech.ring.dotnet.cli/{pkgVer}/tools/net6.0/any/appsettings.json"""

      "Config path should be correct" |> Expect.equal acutalPath expectedPath

    }
  ]
