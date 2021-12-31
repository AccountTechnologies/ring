namespace ATech.Ring.Protocol.v2;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class Queue<T> : ISender<T>, IReceiver<T>
{
    private readonly ConcurrentQueue<T> _queue = new();

    public void Enqueue(T item) => _queue.Enqueue(item);
    public bool TryDequeue(out T item) => _queue.TryDequeue(out item);
    public async Task<T> WaitForNextAsync(CancellationToken token)
    {
        T item;
        var shutdownTimeoutMillis = 5_000;
        const int resolutionMillis = 100;
        while (!_queue.TryDequeue(out item))
        {
            if (token.IsCancellationRequested)
            {
                shutdownTimeoutMillis -= resolutionMillis;
                if (shutdownTimeoutMillis <= 0) token.ThrowIfCancellationRequested();
            }

            await Task.Delay(resolutionMillis, default);
        }
        return item;
    }
}
