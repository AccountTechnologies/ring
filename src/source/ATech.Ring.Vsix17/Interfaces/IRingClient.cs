using ATech.Ring.Protocol.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ATech.Ring.Vsix.Interfaces
{
    public interface IRingClient
    {
        Task<Task> ConnectAsync(Func<IRingEvent, Task> dispatchAsync, CancellationToken token);
        Task DisconnectAsync();
    }
}
