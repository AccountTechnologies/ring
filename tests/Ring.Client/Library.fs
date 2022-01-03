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
  let onRingEvent = new Event<Msg>()

  let cancellationToken =  options.CancelationToken |> Option.defaultValue CancellationToken.None
  let mutable listenTask = Task.CompletedTask
  let socket = lazy(task {
      let mutable s = new ClientWebSocket()
      use connectionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(20))
      while s.State <> WebSocketState.Open && not <| connectionTimeout.IsCancellationRequested do
        try
          do! s.ConnectAsync(options.RingUrl, cancellationToken)
        with
         | :? WebSocketException as ex ->
          s <- new ClientWebSocket()
          do! Task.Delay (TimeSpan.FromSeconds(1))

      listenTask <- s.ListenAsync(WebSocketExtensions.HandleMessage(
      fun m t ->
        onRingEvent.Trigger({Type=m.Type; Payload = m.Bytes.AsUtf8String()})
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

  member _.Terminate() = task {
    let! s = socket.Value
    do! s.SendMessageAsync(M.TERMINATE, cancellationToken)
  }

  member _.Connect() = task {
    let! _ = socket.Value
    ()
  }
  member _.IsConnected = socket.IsValueCreated

  member x.WaitUntilMessage(typ: M, ?suchAs: string -> bool, ?timeout: TimeSpan) =
    try
      x.Event
      |> Observable.iter (fun x -> printfn "%A" x.Type)
      |> Observable.firstIf (fun x ->  x.Type = typ)
      |> Observable.timeout (DateTimeOffset.Now.Add(defaultArg timeout (TimeSpan.FromSeconds(10))))
      |> Observable.filter (fun x -> x.Type = typ)
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
                do! listenTask
              with
               | :? WebSocketException ->
                 printfn "Socket closed"
              let! s = socket.Value
              s.Dispose()
      })  
