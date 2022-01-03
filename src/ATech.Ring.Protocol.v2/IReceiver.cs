namespace ATech.Ring.Protocol.v2;

using System.Threading;
using System.Threading.Tasks;

public delegate Task OnDequeue(Message message);

public interface IReceiver
{
    Task DequeueAsync(OnDequeue action);
    void Complete();
    Task<bool> WaitToReadAsync(CancellationToken token);
}
