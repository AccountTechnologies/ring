namespace ATech.Ring.Protocol.v2;

public interface ISender<in T>
{
    void Enqueue(T item);
}
