module Ring.Tests.Integration.AspNetCore

open Expecto
open FSharp.Control
open FsHttp
open Ring.Client.Patterns
open Ring.Tests.Integration.Async
open Ring.Tests.Integration.RingControl
open Ring.Tests.Integration.Shared
open Ring.Tests.Integration.TestContext

[<Tests>]
let tests =
  testList "AspNetCore runnable tests" [

    testTask "should override url via Urls" {
      use ctx = new TestContext(localOptions >> logToFile "aspnetcore-urls.ring.log")
      let! (ring : Ring, dir: TestDir) = ctx.Init()

      ring.Headless(debugMode=true)
      do! ring.Client.Connect()
      do! ring.Client.LoadWorkspace (dir.InSourceDir "../resources/aspnetcore-urls.toml")
      do! ring.Client.StartWorkspace()
      
      let! healthy =
        ring.Client.NewEvents
        |> AsyncSeq.exists (Runnable.healthy "aspnetcore")
        |> Async.AsTaskTimeout
      
      "Aspnetcore runnable expected healthy" |> Expect.isTrue healthy
      let response =
        http {
          GET "http://localhost:7123"
        }
        |> Request.send
        |> Response.assertOk
        |> Response.toText
      
      "Response on port 7123 should be OK" |> Expect.equal response "OK"
        
      do! ring.Client.Terminate() 
    }
  ] |> testLabel "aspnetcore"
