namespace ATech.Ring.Protocol.v2.Events;

public abstract class RunnableEvent : IRingEvent, IRunnableIds
{
    public static T New<T>(IRunnableIds ids) where T : RunnableEvent, new()
        => new() { UniqueId = ids.UniqueId };

    public string UniqueId { get; set; }
    public abstract M Type { get; }
    public virtual Message AsMessage() => new(Type, UniqueId);
}

public class RunnableStarted : RunnableEvent
{
    public override M Type => M.RUNNABLE_STARTED;
}

public class RunnableStopped : RunnableEvent
{
    public override M Type => M.RUNNABLE_STOPPED;
}
public class RunnableInitiated : RunnableEvent
{
    public override M Type => M.RUNNABLE_INITIATED;
}
public class RunnableHealthCheck : RunnableEvent
{
    public override M Type => M.RUNNABLE_HEALTH_CHECK;
}
public class RunnableHealthy : RunnableEvent
{
    public override M Type => M.RUNNABLE_HEALTHY;
}
public class RunnableRecovering : RunnableEvent
{
    public override M Type => M.RUNNABLE_RECOVERING;
}
public class RunnableDead : RunnableEvent
{
    public override M Type => M.RUNNABLE_UNRECOVERABLE;
}

public class RunnableDestroyed : RunnableEvent
{
    public override M Type => M.RUNNABLE_DESTROYED;
}

public class ServerIdle : IRingEvent
{
    public M Type => M.SERVER_IDLE;
    public Message AsMessage() => new(Type);
}

public class Disconnected : IRingEvent
{
    public M Type => M.DISCONNECTED;
    public Message AsMessage() => new(Type);
}

public class ServerLoaded : IRingEvent
{
    public string WorkspacePath { get; set; }
    public M Type => M.SERVER_LOADED;
    public Message AsMessage() => new(Type, WorkspacePath);
}

public class ServerRunning : IRingEvent
{
    public string WorkspacePath { get; set; }
    public M Type => M.SERVER_RUNNING;
    public Message AsMessage() => new(Type, WorkspacePath);
}

public readonly struct AckEvent : IRingEvent
{
    public AckEvent(string value) => Value = (Ack)System.Enum.Parse(typeof(Ack), value);
    public AckEvent(Ack value) => Value = value;
    public Ack Value { get; }
    public M Type => M.ACK;
    public Message AsMessage() => new(Type, (byte) Value);
}
