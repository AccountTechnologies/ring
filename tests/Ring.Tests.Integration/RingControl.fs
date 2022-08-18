namespace Ring.Test.Integration

open ATech.Ring.Protocol.v2
open Ring.Test.Integration.DotNet
open Ring.Test.Integration.DotNet.Types
open System.Threading
open Ring.Client
open System
open System.Threading.Tasks

module RingControl =

  type Ring(options: Options) =
    let cts = new CancellationTokenSource()
    static let mutable portSequence = 7998
    let port = Interlocked.Increment(&portSequence)
    let mutable ringTask : Task option = None

    let execCore dotnetFunc (args:string list) =
      match options.LocalTool with
      | None -> dotnetFunc "ring" options.WorkingDir args
      | Some _ -> dotnetFunc "dotnet" options.WorkingDir ("ring"::args)
    let execResult = execCore Dotnet.procWithResult
    let exec = execCore Dotnet.proc

    let client = WsClient {
      RingUrl = Uri ($"ws://localhost:{port}/ws?clientId={Guid.NewGuid()}")
      CancelationToken = Some cts.Token
    }

    member _.Install() = task {
      let! _ = Dotnet.installTool options
      ()
    }
    member _.Uninstall() = task {
      let! _ = Dotnet.uninstallTool options
      ()
    }
    member _.Options = options
    member _.ShowConfig() =
      task {
        return! execResult ["show-config"] []
      }
    member _.Headless(?debugMode: bool) =
      match ringTask with
      | Some _ -> failwith "Ring is already running"
      | None -> 
        ringTask <- 
          let args =
            ["--no-logo"; "--port"; port |> string ]
            |> Option.foldBack (fun debugMode args -> if debugMode then "--debug"::args else args) debugMode
          Some(exec ("headless"::args) options.Env) 

    member _.Run(?workspacePath:string, ?debugMode: bool) =
      match ringTask with
      | Some _ -> failwith "Ring is already running"
      | None -> 
        ringTask <-
          let args =
            ["--no-logo"; "--port"; (port |> string); "--startup-delay-seconds"; "10"]
            |> Option.foldBack (fun debugMode args -> if debugMode then "--debug"::args else args) debugMode
            |> Option.foldBack (fun path args -> "-w"::path::args) workspacePath

          Some (exec ("run"::args) options.Env)

    member _.Client = client
    interface IAsyncDisposable with
      member _.DisposeAsync(): ValueTask = ValueTask(
        task {
            if client.HasEverConnected then
              do! client.Terminate()
              client.WaitUntilMessage(M.SERVER_SHUTDOWN) |> ignore
              do! (client :> IAsyncDisposable).DisposeAsync()
            cts.Dispose()
            match ringTask with
            | None -> ()
            | Some t -> do! t
        }
      )
