using System.Threading;
using System.Threading.Tasks;

namespace ATech.Ring.Protocol
{
    public interface IReceiver<T>
    {
        bool TryDequeue(out T item);
        Task<T> WaitForNextAsync(CancellationToken token);
    }
}
