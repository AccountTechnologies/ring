namespace Ring.Client

open ATech.Ring.Protocol.v2
open System
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open FSharp.Control.Reactive

type ClientOptions = {
  RingUrl: Uri
  CancelationToken: CancellationToken option
}

type Msg = {
  Payload: string
  Type: M
}

type WsClient(options: ClientOptions) =
  let onRingEvent = Event<Msg>()

  let cancellationToken =  options.CancelationToken |> Option.defaultValue CancellationToken.None
  let mutable listenTask = Task.CompletedTask
  let mutable terminateRequested = false
  let socket = lazy(task {
      let mutable s = new ClientWebSocket()
      use connectionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(20))
      while s.State <> WebSocketState.Open && not <| connectionTimeout.IsCancellationRequested do
        try
          do! s.ConnectAsync(options.RingUrl, cancellationToken)
        with
         | :? WebSocketException as ex ->
          printfn $"Test client failed to connect to Ring: {ex.Message}. Reconnecting..."
          s.Dispose()
          s <- new ClientWebSocket()
          do! Task.Delay (TimeSpan.FromSeconds(1))

      listenTask <- s.ListenAsync(WebSocketExtensions.HandleMessage(
      fun m t ->
        onRingEvent.Trigger({Type=m.Type; Payload = m.PayloadString})
        Task.CompletedTask
      ), cancellationToken)
      return s
    })
   
  [<CLIEvent>]
  member _.Event = onRingEvent.Publish
  
  member _.LoadWorkspace(path: string) = task {
    let! s = socket.Value
    do! s.SendMessageAsync(Message(M.LOAD, path))
  }
  
  member _.StartWorkspace() = task {
    let! s = socket.Value
    do! s.SendMessageAsync(M.START)
  }

  member _.StopWorkspace() = task {
    let! s = socket.Value
    do! s.SendMessageAsync(M.STOP)
  }

  member _.Terminate() = task {
    if terminateRequested then ()
    else
      terminateRequested <- true
      let! s = socket.Value
      do! s.SendMessageAsync(M.TERMINATE, cancellationToken)
  }

  member _.Connect() = task {
    let! _ = socket.Value
    ()
  }

  member _.IsConnected = socket.IsValueCreated

  member x.WaitUntilMessage(typ: M, ?timeout: TimeSpan) =
    try
      x.Event
      |> Observable.iter (fun x -> printfn "\n----> %A %s <----\n" x.Type x.Payload)
      |> Observable.firstIf (fun x ->  x.Type = typ)
      |> Observable.timeout (DateTimeOffset.Now.Add(defaultArg timeout (TimeSpan.FromSeconds(10))))
      |> Observable.wait
      |> Some
    with
     | :? TimeoutException -> None
     | :? InvalidOperationException as x when x.Message = "Sequence contains no elements." ->
       None

  interface IAsyncDisposable with
    member _.DisposeAsync() = 
      ValueTask (task {
            if socket.IsValueCreated
            then
              try
                let! s = socket.Value
                do! s.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancellationToken)
                do! listenTask
              with
               | :? WebSocketException as wx ->
                 printfn "%s" (wx.ToString())
              let! s = socket.Value
              s.Dispose()
      })  
