namespace ATech.Ring.Protocol.v2;

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public sealed class Queue : ISender, IReceiver
{
    private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>();

    public async Task CompleteAsync(TimeSpan timeout)
    {
        await Task.Delay(timeout);
        _channel.Writer.Complete();
    }

    public void Enqueue(Message item)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(item.Bytes.Length);
        Array.Clear(bytes);
        item.Bytes.CopyTo(bytes);
        _channel.Writer.TryWrite(bytes);
    }

    public async Task<bool> WaitToReadAsync(CancellationToken token)
    {
        try
        {
            return await _channel.Reader.WaitToReadAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public Task DequeueAsync(OnDequeue action)
    {
        if (!_channel.Reader.TryRead(out var bytes)) return Task.CompletedTask;
        try
        {
            return action(new Message(bytes));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes, true);
        }
    }
}
