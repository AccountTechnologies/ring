namespace Ring.Client

open System.IO
open System.Threading.Channels
open ATech.Ring.Protocol.v2
open System
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open ATech.Ring.Protocol.v2.Events
open FSharp.Control
open Newtonsoft.Json

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
  Scope: MsgScope
} and MsgScope = | Server | Workspace | Runnable of id: string | Unknown

module Patterns =
    
  let (|Runnable|_|) =
    function | { Scope = Runnable id } -> Some id | _ -> None
  let (|RunnableHealthy|_|)  =
    function | { Type = M.RUNNABLE_HEALTHY; Payload = p } -> Some p | _ -> None

  let (|RunnableHealthCheck|_|) =
    function | { Type = M.RUNNABLE_HEALTH_CHECK; Payload = p } -> Some p | _ -> None
  
  let (|RunnableInitiated|_|)  =
    function | { Type = M.RUNNABLE_INITIATED; Payload = p } -> Some p | _ -> None
  
  let (|RunnableStarted|_|)  =
    function | { Type = M.RUNNABLE_STARTED; Payload = p } -> Some p | _ -> None
  
  let (|RunnableStopped|_|)  =
    function | { Type = M.RUNNABLE_STOPPED; Payload = p } -> Some p | _ -> None
  
  let (|RunnableDestroyed|_|)  =
    function | { Type = M.RUNNABLE_DESTROYED; Payload = p } -> Some p | _ -> None
  
  let (|RunnableRecovering|_|)  =
    function | { Type = M.RUNNABLE_RECOVERING; Payload = p } -> Some p | _ -> None
  
  
  let (|WorkspaceInfo|_|) (msg:Msg) =
   match msg with
   | {Type = M.WORKSPACE_INFO_PUBLISH} ->
     Some (JsonConvert.DeserializeObject<WorkspaceInfo>(msg.Payload))
   | _ -> None
   
  let (|ServerShutdown|_|) (msg:Msg) =
   match msg with
   | {Type = M.SERVER_SHUTDOWN} ->
     Some ()
   | _ -> None 
   
  type Runnable =

     static member private idOrAny actual expected =
       match expected with
       | Some id -> id = actual
       | _ -> true
     static member healthy(?expectedId) =
       function
       | RunnableHealthy actualId when Runnable.idOrAny actualId expectedId -> true
       | _ -> false
     static member started(?expectedId) =
       function
       | RunnableStarted actualId when Runnable.idOrAny actualId expectedId -> true
       | _ -> false
     static member healthCheck(?expectedId) =
       function
       | RunnableHealthCheck actualId when Runnable.idOrAny actualId expectedId -> true
       | _ -> false
     static member stopped(?expectedId) =
       function
       | RunnableStopped actualId when Runnable.idOrAny actualId expectedId -> true
       | _ -> false
     static member initiated(?expectedId) =
       function
       | RunnableInitiated actualId when Runnable.idOrAny actualId expectedId -> true
       | _ -> false
     static member destroyed(?expectedId) =
       function
       | RunnableDestroyed actualId when Runnable.idOrAny actualId expectedId -> true
       | _ -> false
     
     static member byId expectedId =
       function
       | Runnable x when x = expectedId -> true
       | _ -> false

[<RequireQualifiedAccess>]
module Workspace =
  open Patterns
  let infoLike predicate =
    function | WorkspaceInfo info when predicate info -> true | _ -> false
  
  let info = 
    function | WorkspaceInfo info -> Some info | _ -> None

[<RequireQualifiedAccess>]
module Server =
  open Patterns
  let shutdown =
    function | ServerShutdown _ -> true | _ -> false

type WsClient(options: ClientOptions) =
  let buffer = Channel.CreateUnbounded<Msg>()
  let cache = Channel.CreateUnbounded<Msg>()
  let events =
    buffer.Reader.ReadAllAsync()
    |> AsyncSeq.ofAsyncEnum
    |> AsyncSeq.map (fun m -> cache.Writer.TryWrite(m) |> ignore; m)
  let allEvents = cache.Reader.ReadAllAsync() |> AsyncSeq.ofAsyncEnum |> AsyncSeq.cache
  
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
        let msg =
          {
            Timestamp = DateTime.Now.ToLocalTime()
            Type = m.Type
            Payload = m.PayloadString
            Scope =
              match m.Type with
              | M.RUNNABLE_HEALTHY
              | M.RUNNABLE_HEALTH_CHECK
              | M.RUNNABLE_STARTED
              | M.RUNNABLE_STOPPED
              | M.RUNNABLE_DESTROYED
              | M.RUNNABLE_INITIATED
              | M.RUNNABLE_RECOVERING
              | M.RUNNABLE_UNRECOVERABLE -> MsgScope.Runnable m.PayloadString
              | M.WORKSPACE_INFO_PUBLISH -> Workspace
              | M.SERVER_IDLE
              | M.SERVER_LOADED
              | M.SERVER_RUNNING
              | M.SERVER_SHUTDOWN -> Server
              | _ -> Unknown
          }
        if not <| buffer.Writer.TryWrite(msg)
        then failwithf $"Could not write: %A{msg}"
        Task.CompletedTask
      ), cancellationToken)
      return s
    })

  member _.AllEvents = allEvents

  member _.NewEvents = events
  
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
                cache.Writer.Complete()
                let eventLog =
                  x.AllEvents
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
