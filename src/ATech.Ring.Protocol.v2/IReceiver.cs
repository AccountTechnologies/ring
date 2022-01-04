namespace ATech.Ring.Protocol.v2;

using System;
using System.Threading;
using System.Threading.Tasks;

public delegate Task OnDequeue(Message message);

public interface IReceiver
{
    Task DequeueAsync(OnDequeue action);
    Task CompleteAsync(TimeSpan timeout);
    Task<bool> WaitToReadAsync(CancellationToken token);
}
