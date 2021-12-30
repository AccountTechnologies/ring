namespace Ring.Client

open ATech.Ring.Protocol
open System
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open ATech.Ring.Protocol.Events
open FSharp.Control.Reactive

type ClientOptions = {
  RingUrl: Uri
  CancelationToken: CancellationToken option
}

type WsClient(options: ClientOptions) =
  let onRingEvent = new Event<IRingEvent>()

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

      listenTask <- s.ListenAsync(Func<Message,CancellationToken, Task>(
      fun m t ->
        task {
          onRingEvent.Trigger(m.AsEvent())
        })      
      , cancellationToken)
      return s
    })
   
  [<CLIEvent>]
  member _.Event = onRingEvent.Publish
  
  member _.LoadWorkspace(path: string) = task {
    let! s = socket.Value
    do! s.SendMessageAsync(Message.FromString(M.LOAD, path))
  }
  
  member _.StartWorkspace() = task {
    let! s = socket.Value
    do! s.SendMessageAsync(Message.From(M.START))
  }

  member _.Terminate() = task {
    let! s = socket.Value
    do! s.SendMessageAsync(Message.From M.TERMINATE, cancellationToken)
  }

  member x.WaitUntil<'a when 'a :> IRingEvent>(?suchAs: 'a -> bool, ?timeout: TimeSpan) =
    try
      x.Event
      |> Observable.firstIf (fun x -> x.GetType() = typeof<'a>)
      |> Observable.map (fun x -> x :?> 'a) 
      |> Observable.timeout (DateTimeOffset.Now.Add(defaultArg timeout (TimeSpan.FromSeconds(10))))
      |> Observable.filter (defaultArg suchAs (fun _ -> true))
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
