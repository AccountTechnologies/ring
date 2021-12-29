
namespace Ring.Test.Integration

open Ring.Test.Integration.DotNet
open Ring.Test.Integration.DotNet.Types
open System.Threading
open Ring.Client
open System
open System.Threading.Tasks

module RingControl =

  type Ring(options: Options) =
    let cts = new CancellationTokenSource()
    static let mutable portSequence : int = 7998
    static let nextPort () = Interlocked.Increment(&portSequence)
    let port = (nextPort ())
    
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
        return! execResult ["show-config"]
      }
    member _.Headless() =
      task {
        let! _ = exec ["headless"; "--no-logo"; "--port"; port |> string] 
        ()
      }
    member _.Client = client
    interface IAsyncDisposable with
      member _.DisposeAsync(): ValueTask = ValueTask(
        task {
            do! client.Terminate()
            do! (client :> IAsyncDisposable).DisposeAsync()
            cts.Dispose()
        }
      )
