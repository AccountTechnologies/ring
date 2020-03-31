using System.Collections.Concurrent;

namespace ATech.Ring.Protocol
{
    public class Queue<T> : ISender<T>, IReceiver<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        public void Enqueue(T item) => _queue.Enqueue(item);
        public bool TryDequeue(out T item) => _queue.TryDequeue(out item);
    }
}
