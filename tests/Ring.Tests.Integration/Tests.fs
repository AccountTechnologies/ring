module Tests

open Expecto
open Fake.Core
open Ring.Test.Integration.DotNet.Types
open Ring.Test.Integration.RingControl
open ATech.Ring.Protocol.v2
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
  Env = []
}

let withEnv vars options =
  {
    options with Env = vars
  }
  
let logToFile fileName = withEnv [
  "Serilog__WriteTo__0__Name", "File"
  "Serilog__WriteTo__0__Args__path", Path.Combine(Directory.GetCurrentDirectory(), fileName)
  "Serilog__WriteTo__0__Args__outputTemplate", "{Timestamp:HH:mm:ss.fff}|{Level:u3}|{Phase}|{UniqueId}|{Message}{NewLine}{Exception}"
  "Serilog__WriteTo__1__Name", ""
]


let globalOptions (dir:TestDir) = { localOptions dir with LocalTool = None}

let expectEvent (ring:Ring) timeout (typ:M) (maybePayload:string option) =
  let event = ring.Client.WaitUntilMessage(typ, timeout = timeout)

  let runnable = $"Should receive a {typ} message (within {timeout})" |> Expect.wantSome event
  maybePayload |> Option.iter( fun p -> $"Runnable Id should be correct" |> Expect.equal runnable.Payload p)

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
      let expectEvent = expectEvent ring (TimeSpan.FromSeconds(60))
      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/netcore.toml")
      do! ring.Client.StartWorkspace()
        
      Some "k8s-debug-poc" |> expectEvent M.RUNNABLE_INITIATED
      Some "k8s-debug-poc" |> expectEvent M.RUNNABLE_STARTED
      Some "k8s-debug-poc" |> expectEvent M.RUNNABLE_HEALTH_CHECK
      Some "k8s-debug-poc" |> expectEvent M.RUNNABLE_HEALTHY
      do! ring.Client.Terminate()
      Some "k8s-debug-poc" |> expectEvent M.RUNNABLE_STOPPED
      Some "k8s-debug-poc" |> expectEvent M.RUNNABLE_DESTROYED
    }

    testTask "discover and run default workspace config if exists" {
      use ctx = new TestContext(localOptions >> logToFile "run-default.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      File.WriteAllLines(dir.WorkPath + "/ring.toml", [
        "[[aspnetcore]]"
        $"""csproj = '{dir.InSourceDir "../resources/apps/aspnetcore/aspnetcore.csproj"}' """
      ])

      let expectEvent = expectEvent ring (TimeSpan.FromSeconds(60))
      let task = ring.Client.Connect()
      ring.Run(debugMode=true)
      Some "aspnetcore" |> expectEvent M.RUNNABLE_INITIATED
      Some "aspnetcore" |> expectEvent M.RUNNABLE_STARTED
      Some "aspnetcore" |> expectEvent M.RUNNABLE_HEALTH_CHECK
      Some "aspnetcore" |> expectEvent M.RUNNABLE_HEALTHY
      
      do! ring.Client.StopWorkspace()
      Some "aspnetcore" |> expectEvent M.RUNNABLE_STOPPED
      Some "aspnetcore" |> expectEvent M.RUNNABLE_DESTROYED
      do! task
    }
  ]
