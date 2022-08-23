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
