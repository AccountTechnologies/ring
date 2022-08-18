using System.Threading;
using System.Threading.Tasks;

namespace ATech.Ring.Protocol.v2;

public interface ISender
{
    ValueTask EnqueueAsync(Message message, CancellationToken token);
    void Enqueue(Message message);
}
