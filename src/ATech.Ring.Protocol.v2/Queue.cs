namespace ATech.Ring.Protocol.v2;

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public class Queue : ISender, IReceiver
{
    private readonly Channel<byte[]> _channel = Channel.CreateUnbounded<byte[]>();

    public void Complete() => _channel.Writer.Complete();

    public void Enqueue(Message item)
    {
        _channel.Writer.TryWrite(item.Bytes.ToArray());
    }

    public async Task<bool> WaitToReadAsync(CancellationToken token) => await _channel.Reader.WaitToReadAsync(token);

    public Message Dequeue()
    {
        var success = _channel.Reader.TryRead(out var bytes);
        return success ? new Message(bytes) : Message.Empty();
    }
}
