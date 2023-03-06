module Ring.Tests.Integration.WorkspaceConfig

open Expecto
open FSharp.Control
open Ring.Client
open Ring.Tests.Integration.Async
open Ring.Tests.Integration.RingControl
open Ring.Tests.Integration.TestContext
open Ring.Tests.Integration.Shared
open ATech.Ring.Protocol.v2.Events

[<Tests>]
let tests =
  testList "Workspace config tests" [
   
    testTask "import - classic (TOML array of tables)" {
      use ctx = new TestContext(localOptions >> logToFile "workspace.import.classic.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/import-classic.toml")
      let! result =
        ring.Stream
        |> AsyncSeq.choose Workspace.info
        |> AsyncSeq.tryFirst
        |> Async.AsTaskTimeout
      let ws : WorkspaceInfo = "Expected some workspace" |> Expect.wantSome result
      "Workspace should contain a runnable"
      |> Expect.containsAll (ws.Runnables |> Seq.map (fun x -> x.Id)) ["k8s-debug-poc"]
     }
    
    testTask "import - simple (TOML array)" {
      use ctx = new TestContext(localOptions >> logToFile "workspace.import.simple.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/import-simple.toml")

      let! result =
        ring.Stream
        |> AsyncSeq.choose Workspace.info
        |> AsyncSeq.tryFirst
        |> Async.AsTaskTimeout
      
      let ws : WorkspaceInfo = "Expected workspace info" |> Expect.wantSome result
      "Workspace should contain 2 runnables"
      |> Expect.containsAll (ws.Runnables |> Seq.map(fun x -> x.Id)) ["k8s-debug-poc"; "dummy"]
      
      
    }
  ] |> testLabel "workspace-config"
