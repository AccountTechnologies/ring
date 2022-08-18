namespace Ring.Client

open System.IO
open ATech.Ring.Protocol.v2
open System
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open FSharp.Control

type ClientOptions = {
  RingUrl: Uri
  CancellationToken: CancellationToken option
  ClientId: Guid
  LogOutputDir: string
}

type Msg = {
  Timestamp: DateTime
  Payload: string
  Type: M
}

type WsClient(options: ClientOptions) =
  let buffer = Channels.Channel.CreateUnbounded<Msg>()
  
  let mutable eventLog = AsyncSeq.empty
  
  let cancellationToken =  options.CancellationToken |> Option.defaultValue CancellationToken.None
  let mutable listenTask = Task.CompletedTask
  let mutable terminateRequested = false
  let socket = lazy(task {
      let mutable s = new ClientWebSocket()
      use connectionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(20))
      while s.State <> WebSocketState.Open && not <| connectionTimeout.IsCancellationRequested do
        try
          do! s.ConnectAsync(Uri(options.RingUrl, $"ws?clientId={options.ClientId}"), cancellationToken)
        with
         | :? WebSocketException as ex ->
          printfn $"Test client failed to connect to Ring: {ex.Message}. Reconnecting..."
          s.Dispose()
          s <- new ClientWebSocket()
          do! Task.Delay (TimeSpan.FromSeconds(1))

      listenTask <- s.ListenAsync(WebSocketExtensions.HandleMessage(
      fun m t ->
        let msg = { Timestamp = DateTime.Now.ToLocalTime(); Type = m.Type; Payload = m.PayloadString }
        if not <| buffer.Writer.TryWrite(msg)
        then failwithf $"Could not write: %A{msg}"
        Task.CompletedTask
      ), cancellationToken)
      return s
    })

  member _.EventStream =
    eventLog <-
      buffer.Reader.ReadAllAsync() |> AsyncSeq.ofAsyncEnum
      |> AsyncSeq.append eventLog
      |> AsyncSeq.cache
    eventLog

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

  member _.HasEverConnected = socket.IsValueCreated
  
  member x.WaitUntilMessage(typ: M, ?timeout: TimeSpan) : Msg option =
    try
      let waitUntil = defaultArg timeout (TimeSpan.FromSeconds(10))
      let asyncFlow =
        x.EventStream
        |> AsyncSeq.skipWhile (fun x ->  x.Type <> typ)
        |> AsyncSeq.tryFirst
      Async.RunSynchronously(asyncFlow, waitUntil.TotalMilliseconds |> int)
      
    with
     | :? TimeoutException -> None
     | :? InvalidOperationException as x when x.Message = "Sequence contains no elements." ->
       None

  interface IAsyncDisposable with
    member x.DisposeAsync() = 
      ValueTask (task {
            if socket.IsValueCreated
            then
              try
                let! s = socket.Value
                do! s.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancellationToken)
                do! listenTask
                buffer.Writer.Complete()
                let eventLog =
                  x.EventStream
                  |> AsyncSeq.map (fun m ->
                    match m with
                    | x when x.Payload = "" -> m.Type |> string
                    | x -> $"%A{x.Type}|%s{x.Payload}"
                    |> fun pretty -> $"{m.Timestamp:``HH:mm:ss.fff``}|{pretty}"
                   )
                  |> AsyncSeq.toListAsync
                let log = Async.RunSynchronously(eventLog, 10000)
                Directory.CreateDirectory(options.LogOutputDir) |> ignore
                File.AppendAllLines($"{options.LogOutputDir}/{options.ClientId}.client.log", log)
              with
               | :? WebSocketException as wx ->
                 printfn $"%s{wx.ToString()}"
              let! s = socket.Value
              s.Dispose()
      })  
