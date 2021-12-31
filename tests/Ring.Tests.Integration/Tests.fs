module Tests

open Expecto
open Fake.Core
open Ring.Test.Integration.DotNet.Types
open Ring.Test.Integration.RingControl
open ATech.Ring.Protocol.Events
open System
open Ring.Test.Integration.TestContext
open System.IO

let env name = Environment.environVar name

let localOptions (dir:TestDir) = {
  SrcPath = Environment.environVarOrDefault "SRC_PATH" (dir.InSourceDir "../../src/ATech.Ring.DotNet.Cli")
  PackageVersion = Environment.environVarOrDefault "PKGVER" "0.0.0-dev"
  NuGetSourcePath = Environment.environVarOrDefault "BUILD_ARTIFACTSTAGINGDIRECTORY" (dir.InSourceDir "../../src/ATech.Ring.DotNet.Cli/bin/Release")
  NuGetName = "atech.ring.dotnet.cli"
  NuGetConfigPath = dir.InSourceDir "../resources/NuGet.config"
  LocalTool = Some {
    InstallPath = dir.WorkPath
    ManifestFilePath = ".config/dotnet-tools.json"
  }
  WorkingDir = dir.WorkPath
}

let globalOptions (dir:TestDir) = { localOptions dir with LocalTool = None}

[<Tests>]
let tests =
  testList "Smoke tests" [
    testTask "show-config -- global" {
      use ctx = new TestContext(globalOptions)
      let! (ring : Ring, _) = ctx.Init()
      let pkgVer = ring.Options.PackageVersion
      let! actualPath = ring.ShowConfig()
      let expectedPath =
        if Environment.isWindows
        then $"""{env "USERPROFILE"}\.dotnet\tools\.store\atech.ring.dotnet.cli\{pkgVer}\atech.ring.dotnet.cli\{pkgVer}\tools\net6.0\any\appsettings.json"""
        else $"""{env "HOME"}/.dotnet/tools/.store/atech.ring.dotnet.cli/{pkgVer}/atech.ring.dotnet.cli/{pkgVer}/tools/net6.0/any/appsettings.json"""
      "Config path should be correct" |> Expect.equal actualPath expectedPath
    }

    testTask "show-config -- local" {
      use ctx = new TestContext(localOptions)
      let! (ring : Ring, _) = ctx.Init()
      let pkgVer = ring.Options.PackageVersion
      let! actualPath = ring.ShowConfig()
      let expectedPath =
        if Environment.isWindows
        then $"""{env "USERPROFILE"}\.nuget\packages\atech.ring.dotnet.cli\{pkgVer}\tools\net6.0\any\appsettings.json"""
        else $"""{env "HOME"}/.nuget/packages/atech.ring.dotnet.cli/{pkgVer}/tools/net6.0/any/appsettings.json"""

      "Config path should be correct" |> Expect.equal actualPath expectedPath
    }

    testTask "run basic workspace in headless mode" {
      use ctx = new TestContext(localOptions)
      let! (ring : Ring, dir: TestDir) = ctx.Init()
      ring.Headless()
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/netcore.toml")
      do! ring.Client.StartWorkspace()
      let runnableId = "k8s-debug-poc"
      let timeout = TimeSpan.FromSeconds(60)
      let event = ring.Client.WaitUntil<RunnableHealthy>(suchAs = (fun x -> x.UniqueId = runnableId), timeout = timeout)
      $"Should receive RunnableHealthy for k8s-debug-poc (within {timeout})" |> Expect.isSome event
    }

    testTask "discover and run default workspace config if exists" {
      use ctx = new TestContext(localOptions)
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      File.WriteAllLines(dir.WorkPath + "/ring.toml", [
        "[[aspnetcore]]"
        $"""csproj = '{dir.InSourceDir "../resources/apps/aspnetcore/aspnetcore.csproj"}' """
      ])

      let runnableId = "aspnetcore"
      let timeout = TimeSpan.FromSeconds(60)

      ring.Run()
      do! ring.Client.Connect()
      let event = ring.Client.WaitUntil<RunnableHealthy>(suchAs = (fun x -> x.UniqueId = runnableId), timeout = timeout)
      $"Should receive RunnableHealthy for {runnableId} (within {timeout})" |> Expect.isSome event
    }
  ]