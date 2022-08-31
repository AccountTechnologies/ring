module Ring.Tests.Integration.Proc

open System.Threading.Tasks
open Expecto
open FSharp.Control
open Ring.Client.Patterns
open Ring.Tests.Integration.Async
open Ring.Tests.Integration.RingControl
open Ring.Tests.Integration.Shared
open Ring.Tests.Integration.TestContext

[<Tests>]
let tests =
  testList "Process runnable tests" [

    testTask "should run process" {
      use ctx = new TestContext(localOptions >> logToFile "proc-simple.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/basic/proc.toml")
      do! ring.Client.StartWorkspace()
      
      let proc1Healthy = 
        ring.Stream
        |> AsyncSeq.exists (Runnable.healthy "proc-1")
        |> Async.AsTaskTimeout 
      
      let proc2Healthy = 
        ring.Stream
        |> AsyncSeq.exists (Runnable.healthy "proc-2")
        |> Async.AsTaskTimeout
      do! Task.WhenAll([|proc1Healthy; proc2Healthy|])
      let! proc1Result = proc1Healthy
      "Proc 1 should be healthy" |> Expect.isTrue proc1Result
      let! proc2Result = proc2Healthy
      "Proc 2 should be healthy" |> Expect.isTrue proc2Result       
     }
  ] |> testLabel "proc"
