module Ring.Tests.Integration.AspNetCore

open Expecto

open FsHttp
open FsHttp.Dsl
open FsHttp.DslCE
open Ring.Tests.Integration.RingControl
open ATech.Ring.Protocol.v2
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
      
      ring.waitUntilHealthy "aspnetcore"
      
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
