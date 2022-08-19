module Ring.Tests.Integration.Smoke

open Expecto
open Fake.Core
open Ring.Tests.Integration.DotNet.Types
open Ring.Tests.Integration.RingControl
open ATech.Ring.Protocol.v2
open Ring.Tests.Integration.Shared
open Ring.Tests.Integration.TestContext
open System.IO

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
      use ctx = new TestContext(localOptions >> logToFile "run-basic.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/netcore.toml")
      do! ring.Client.StartWorkspace()
      [
        M.RUNNABLE_INITIATED
        M.RUNNABLE_STARTED
        M.RUNNABLE_HEALTH_CHECK
        M.RUNNABLE_HEALTHY
      ]
      |> Seq.map ring.expect
      |> Expect.forId "k8s-debug-poc"
      
      do! ring.Client.Terminate()
      
      [
        M.RUNNABLE_STOPPED
        M.RUNNABLE_DESTROYED
      ]
      |> Seq.map ring.expect
      |> Expect.forId "k8s-debug-poc"  
    }

    testTask "discover and run default workspace config if exists" {
      use ctx = new TestContext(localOptions >> logToFile "run-default.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      File.WriteAllLines(dir.WorkPath + "/ring.toml", [
        "[[aspnetcore]]"
        $"""csproj = '{dir.InSourceDir "../resources/apps/aspnetcore/aspnetcore.csproj"}' """
      ])


      let task = ring.Client.Connect()
      ring.Run(debugMode=true)
      [
        M.RUNNABLE_INITIATED 
        M.RUNNABLE_STARTED
        M.RUNNABLE_HEALTH_CHECK
        M.RUNNABLE_HEALTHY
      ]
      |> Seq.map ring.expect
      |> Expect.forId "aspnetcore"
      
      do! ring.Client.StopWorkspace()
      [
        M.RUNNABLE_STOPPED
        M.RUNNABLE_DESTROYED
      ]
      |> Seq.map ring.expect
      |> Expect.forId "aspnetcore"
      do! task
    }
  ]
