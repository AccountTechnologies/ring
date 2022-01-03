namespace ATech.Ring.Protocol.v2;

public interface ISender
{
    void Enqueue(Message message);
}
