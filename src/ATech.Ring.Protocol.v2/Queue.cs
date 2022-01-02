namespace ATech.Ring.Protocol.v2;

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public class Queue<T> : ISender<T>, IReceiver<T>
{
    private readonly Channel<T, T> _channel = Channel.CreateUnbounded<T>();

    public void Enqueue(T item) => _channel.Writer.TryWrite(item);
    public bool TryDequeue(out T item) => _channel.Reader.TryRead(out item);
    public async Task<T> WaitForNextAsync(CancellationToken token)
    {
        T item;
        var shutdownTimeoutMillis = 5_000;
        const int resolutionMillis = 100;
        while (!TryDequeue(out item))
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
