module Ring.Tests.Integration.WorkspaceConfig

open ATech.Ring.Protocol.v2.Events
open Expecto
open Newtonsoft.Json
open Ring.Tests.Integration.RingControl
open ATech.Ring.Protocol.v2
open Ring.Tests.Integration.TestContext
open Ring.Tests.Integration.Shared

[<Tests>]
let tests =
  testList "Workspace config tests" [
   
    testTask "import - classic (TOML array of tables)" {
      use ctx = new TestContext(localOptions >> logToFile "workspace.import.classic.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/import-classic.toml")

      M.WORKSPACE_INFO_PUBLISH
      |> ring.expect
      |> fun (o, t) -> $"Want '{t}' message" |> Expect.wantSome o
      |> fun m -> JsonConvert.DeserializeObject<WorkspaceInfo>(m.Payload)
      |> fun m ->
        "Should have one runnable" |> Expect.hasCountOf m.Runnables 1u (fun x -> x.Id = "k8s-debug-poc")
    }
    
    testTask "import - simple (TOML array)" {
      use ctx = new TestContext(localOptions >> logToFile "workspace.import.simple.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/import-simple.toml")

      M.WORKSPACE_INFO_PUBLISH
      |> ring.expect
      |> fun (o, t) -> $"Want '{t}' message" |> Expect.wantSome o
      |> fun m -> JsonConvert.DeserializeObject<WorkspaceInfo>(m.Payload)
      |> fun m ->
        "Should have runnable" |> Expect.hasCountOf m.Runnables 1u (fun x -> x.Id = "k8s-debug-poc")
        "Should have runnable" |> Expect.hasCountOf m.Runnables 1u (fun x -> x.Id = "dummy")
    }
  ] |> testLabel "workspace-config"
