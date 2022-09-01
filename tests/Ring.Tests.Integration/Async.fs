module Ring.Tests.Integration.Async

open System
open System.Threading

type Async with
  
  static member AsTaskTimeout (computation: Async<'a>) =
    async {
      let! r = Async.StartChild(computation, millisecondsTimeout = 30000)
      return! r
    }
    |> Async.StartAsTask
    
    
    
   
