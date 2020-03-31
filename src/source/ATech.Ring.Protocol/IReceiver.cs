namespace ATech.Ring.Protocol
{
    public interface IReceiver<T>
    {
        bool TryDequeue(out T item);
    }
}
