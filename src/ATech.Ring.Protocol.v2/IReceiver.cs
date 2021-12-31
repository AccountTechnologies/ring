namespace ATech.Ring.Protocol.v2;

using System.Threading;
using System.Threading.Tasks;

public interface IReceiver<T>
{
    bool TryDequeue(out T item);
    Task<T> WaitForNextAsync(CancellationToken token);
}
