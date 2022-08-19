namespace Ring.Tests.Integration

open System.IO
open System
open Ring.Tests.Integration.DotNet.Types
open Ring.Tests.Integration.RingControl
open System.Threading.Tasks

module TestContext =
  
  type TestDir() =
    let origDir = 
      let cwd = Directory.GetCurrentDirectory()
      // ugly hack for https://github.com/microsoft/vstest/issues/2004
      if Path.Combine(cwd, "../resources/NuGet.config") |> Path.GetFullPath |> File.Exists then cwd
      else $"{cwd}/../../../" |> Path.GetFullPath
  
    let dir =
      let d = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()) |> Directory.CreateDirectory
      d
    member _.WorkPath = dir.FullName
    member _.InSourceDir path = Path.Combine(origDir, path) |> Path.GetFullPath
    interface IDisposable with
      member _.Dispose(): unit =
        dir.Delete(true)

  type TestContext(opts: TestDir -> Options) =
    let dir = new TestDir()
    let ring = Ring(dir |> opts)

    member _.Init() =
      task {
          do! ring.Install()
          return ring, dir
      }
    
    interface IAsyncDisposable with
      member _.DisposeAsync(): ValueTask = 
        ValueTask (
            task {
                do! (ring :> IAsyncDisposable).DisposeAsync()
                do! ring.Uninstall()
                (dir :> IDisposable).Dispose()
            }
            )
    interface IDisposable with
      member this.Dispose(): unit = 
        (this :> IAsyncDisposable).DisposeAsync().GetAwaiter().GetResult()
