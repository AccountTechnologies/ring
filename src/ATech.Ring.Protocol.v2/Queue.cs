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

    private static byte[] CopyBytes(Message message)
    {
        var bytes = ArrayPool<byte>.Shared.Rent(message.Bytes.Length);
        Array.Clear(bytes);
        message.Bytes.CopyTo(bytes);
        return bytes;
    }
    public ValueTask EnqueueAsync(Message message, CancellationToken token)
    {
        return _channel.Writer.WriteAsync(CopyBytes(message), token);
    }

    public void Enqueue(Message message)
    {
        _channel.Writer.TryWrite(CopyBytes(message));
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
