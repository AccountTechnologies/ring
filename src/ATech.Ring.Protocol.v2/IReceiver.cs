namespace ATech.Ring.Protocol.v2;

using System.Threading;
using System.Threading.Tasks;

public interface IReceiver
{
    Message Dequeue();
    Task<bool> WaitToReadAsync(CancellationToken token);
}
