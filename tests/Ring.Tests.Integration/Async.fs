module Ring.Tests.Integration.Async

open System
open System.Threading

type Async with
  
  static member AsTaskTimeout (computation: Async<'a>) =
    try
      use cts = new CancellationTokenSource(TimeSpan.FromSeconds(60))
      Async.StartAsTask(computation, cancellationToken = cts.Token)
     with
     | :? OperationCanceledException ->
       failwithf "Gave up waiting after 1 minute"
