namespace ATech.Ring.Protocol
{
    public interface ISender<in T>
    {
        void Enqueue(T item);
    }
}
