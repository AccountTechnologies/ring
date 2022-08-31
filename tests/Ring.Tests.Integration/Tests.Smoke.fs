module Ring.Tests.Integration.Smoke

open ATech.Ring.Protocol.v2
open Expecto
open FSharp.Control
open Fake.Core
open Ring.Client
open Ring.Tests.Integration.DotNet.Types
open Ring.Tests.Integration.Async
open Ring.Tests.Integration.RingControl
open Ring.Tests.Integration.Shared
open Ring.Client.Patterns
open Ring.Tests.Integration.TestContext
open System.IO

[<Tests>]
let tests =
  testList "Smoke tests" [
    testTask "config-path -- global tool" {
      use ctx = new TestContext(globalOptions)
      let! (ring : Ring, _) = ctx.Init()
      let pkgVer = ring.Options.PackageVersion
      let! actualPath = ring.ConfigPath("--default")
      let expectedPath =
        if Environment.isWindows
        then $"""{env "USERPROFILE"}\.dotnet\tools\.store\atech.ring.dotnet.cli\{pkgVer}\atech.ring.dotnet.cli\{pkgVer}\tools\net6.0\any\app.windows.toml"""
        else if Environment.isMacOS
        then $"""{env "HOME"}/.dotnet/tools/.store/atech.ring.dotnet.cli/{pkgVer}/atech.ring.dotnet.cli/{pkgVer}/tools/net6.0/any/app.osx.toml"""
        else $"""{env "HOME"}/.dotnet/tools/.store/atech.ring.dotnet.cli/{pkgVer}/atech.ring.dotnet.cli/{pkgVer}/tools/net6.0/any/app.linux.toml"""
      "Config path should be correct" |> Expect.equal actualPath expectedPath
    }

    testTask "config-path -- local tool" {
      use ctx = new TestContext(localOptions)
      let! (ring : Ring, dir:TestDir) = ctx.Init()
      let pkgVer = ring.Options.PackageVersion
      let! actualPath = ring.ConfigPath("--default")
      let expectedPath =
        if Environment.isWindows
        then $"""{env "USERPROFILE"}\.nuget\packages\atech.ring.dotnet.cli\{pkgVer}\tools\net6.0\any\app.windows.toml"""
        else if Environment.isMacOS
        then $"""{env "HOME"}/.nuget/packages/atech.ring.dotnet.cli/{pkgVer}/tools/net6.0/any/app.osx.toml"""
        else $"""{env "HOME"}/.nuget/packages/atech.ring.dotnet.cli/{pkgVer}/tools/net6.0/any/app.linux.toml"""
        
      "Config path should (default) be correct" |> Expect.equal actualPath expectedPath
      
      let! actualPath = ring.ConfigPath("--user")
      let expectedPath =
       if Environment.isWindows
        then $"""{env "USERPROFILE"}\AppData\Roaming\.ring\settings.toml"""
        else $"""{env "HOME"}/.config/.ring/settings.toml"""
      "Config path should (user) be correct" |> Expect.equal actualPath expectedPath
      
      let! actualPath = ring.ConfigPath("--local")
      let expectedPath =
       if Environment.isWindows
        then $"""{dir.WorkPath}\.ring\settings.toml"""
        else if Environment.isMacOS then $"""/private{dir.WorkPath}/.ring/settings.toml"""
        else $"""{dir.WorkPath}/.ring/settings.toml"""
      "Config path should (local) be correct" |> Expect.equal actualPath expectedPath
    }

    testTask "run basic workspace in headless mode" {
      use ctx = new TestContext(localOptions >> logToFile "run-basic.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/netcore.toml")
      do! ring.Client.StartWorkspace()
      
      let! events = (ring.Stream
        |> AsyncSeq.filter (Runnable.byId "k8s-debug-poc")
        |> AsyncSeq.takeWhileInclusive(not << Runnable.healthy())
        |> AsyncSeq.map (fun m -> m.Type)
        |> AsyncSeq.toListAsync
        |> Async.AsTaskTimeout)
      
      "Unexpected events sequence" |> Expect.sequenceEqual events [
        M.RUNNABLE_INITIATED
        M.RUNNABLE_STARTED
        M.RUNNABLE_HEALTH_CHECK
        M.RUNNABLE_HEALTHY
      ]
      
      do! ring.Client.Terminate()
      
      let! events = (ring.Stream
        |> AsyncSeq.filter (Runnable.byId "k8s-debug-poc")
        |> AsyncSeq.takeWhileInclusive(not << Runnable.destroyed())
        |> AsyncSeq.map (fun m -> m.Type)
        |> AsyncSeq.toListAsync
        |> Async.AsTaskTimeout)
      
      "Unexpected events sequence" |> Expect.sequenceEqual events [
        M.RUNNABLE_STOPPED
        M.RUNNABLE_DESTROYED
      ] 
    }

    testTask "discover and run default workspace config if exists" {
      use ctx = new TestContext(localOptions >> logToFile "run-default.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      File.WriteAllLines(dir.WorkPath + "/ring.toml", [
        "[[aspnetcore]]"
        $"""csproj = '{dir.InSourceDir "../resources/apps/aspnetcore/aspnetcore.csproj"}' """
      ])

     
      ring.Run(debugMode=true)
      do! ring.Client.Connect()
            
      let! events = (ring.Stream
        |> AsyncSeq.filter (Runnable.byId "aspnetcore")
        |> AsyncSeq.takeWhileInclusive(not << Runnable.healthy())
        |> AsyncSeq.map (fun m -> m.Type)
        |> AsyncSeq.toListAsync
        |> Async.AsTaskTimeout)
      
      "Unexpected events sequence" |> Expect.sequenceEqual events [
        M.RUNNABLE_INITIATED
        M.RUNNABLE_STARTED
        M.RUNNABLE_HEALTH_CHECK
        M.RUNNABLE_HEALTHY
      ]
      
      do! ring.Client.StopWorkspace()
      
      let! events = (ring.Stream
        |> AsyncSeq.filter (Runnable.byId "aspnetcore")
        |> AsyncSeq.takeWhileInclusive(not << Runnable.destroyed())
        |> AsyncSeq.map (fun m -> m.Type)
        |> AsyncSeq.toListAsync
        |> Async.AsTaskTimeout)
      
      "Unexpected events sequence" |> Expect.sequenceEqual events [
        M.RUNNABLE_STOPPED
        M.RUNNABLE_DESTROYED
      ]
    }
  ]
