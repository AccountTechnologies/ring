namespace ATech.Ring.Protocol.v2;

using ATech.Ring.Protocol.v2.Events;
using System;
using System.Text;

public enum M : byte
{
    DISCONNECTED = 0,
    LOAD = 62,
    UNLOAD = 60,
    START = 35,
    STOP = 36,
    TERMINATE = 81,
    RUNNABLE_INCLUDE = 43,
    INCLUDE_ALL = 44,
    RUNNABLE_EXCLUDE = 45,
    EXCLUDE_ALL = 46,
    ACK = 58,
    PING = 2,
    WORKSPACE_INFO_RQ = 63,
    RUNNABLE_INITIATED = 11,
    RUNNABLE_STARTED = 12,
    RUNNABLE_STOPPED = 13,
    RUNNABLE_HEALTH_CHECK = 14,
    RUNNABLE_HEALTHY = 15,
    RUNNABLE_UNRECOVERABLE = 16,
    RUNNABLE_RECOVERING = 17,
    RUNNABLE_DESTROYED = 18,

    WORKSPACE_DEGRADED = 19,
    WORKSPACE_HEALTHY = 20,
    WORKSPACE_STOPPED = 21,
    WORKSPACE_INFO_PUBLISH = 26,
    SERVER_IDLE = 22,
    SERVER_LOADED = 23,
    SERVER_RUNNING = 24
}

public interface IAsMessage
{
    Message AsMessage();
    M Type { get; }
}

public static class ReadOnlySpanExtensions
{
    public static string AsUtf8String(this ReadOnlySpan<byte> span) => Encoding.UTF8.GetString(span);
}

public ref struct Message
{
    public ReadOnlySpan<byte> Bytes { get; }
    public M Type => (M)Bytes[0];

    public Message(ReadOnlySpan<byte> bytes) => Bytes = bytes;
    public Message(M type, string value) : this(type, Encoding.UTF8.GetBytes(value)) { }
    public Message(M type, byte value) : this(type, new byte[] {value}) { }
    public Message(M type, ReadOnlySpan<byte> bytes)
    {
        byte[] newBytes = new byte[bytes.Length + 1];
        newBytes[0] = (byte)type;
        Array.Copy(bytes.ToArray(), 0, newBytes, 1, bytes.Length);
        Bytes = newBytes;
    }
    public Message(M type) => Bytes = new byte[] { (byte)type };

    public void Deconstruct(out M type, out ReadOnlySpan<byte> payload)
    {
        type = Type;
        payload = Bytes.Length == 1 ? ReadOnlySpan<byte>.Empty : SliceUntilNull(Bytes[1..]);
    }

    public override string ToString()
    {
        var (type, payload) = this;
        var maybeColon = payload == ReadOnlySpan<byte>.Empty ? "" : ":";
        return $"{type}{maybeColon}{payload.AsUtf8String()}";
    }

    private static ReadOnlySpan<byte> SliceUntilNull(ReadOnlySpan<byte> m)
    {
        var nullChar = m.IndexOf((byte)0);
        return m[..(nullChar == -1 ? m.Length : nullChar)];
    }

    public static implicit operator ReadOnlySpan<byte>(Message m) => m.Bytes;
    public static implicit operator Message(ReadOnlySpan<byte> bytes) => new(bytes);
    public static implicit operator Message(M type) => new(type);
}

public static class FromMessage
{
    public static IRingEvent AsEvent(this Message m) => m switch
    {
        (M.ACK, var ack) => new AckEvent((Ack)ack[0]),
        (M.RUNNABLE_INITIATED, var runnableId) => new RunnableInitiated { UniqueId = runnableId.AsUtf8String() },
        (M.RUNNABLE_STARTED, var runnableId) => new RunnableStarted { UniqueId = runnableId.AsUtf8String() },
        (M.RUNNABLE_UNRECOVERABLE, var runnableId) => new RunnableDead { UniqueId = runnableId.AsUtf8String() },
        (M.RUNNABLE_HEALTH_CHECK, var runnableId) => new RunnableHealthCheck { UniqueId = runnableId.AsUtf8String() },
        (M.RUNNABLE_HEALTHY, var runnableId) => new RunnableHealthy { UniqueId = runnableId.AsUtf8String() },
        (M.RUNNABLE_RECOVERING, var runnableId) => new RunnableRecovering { UniqueId = runnableId.AsUtf8String() },
        (M.RUNNABLE_STOPPED, var runnableId) => new RunnableStopped { UniqueId = runnableId.AsUtf8String() },
        (M.RUNNABLE_DESTROYED, var runnableId) => new RunnableDestroyed { UniqueId = runnableId.AsUtf8String() },
        (M.WORKSPACE_INFO_PUBLISH, var data) => new WorkspaceInfoPub(data),
        (M.SERVER_IDLE, _) => new ServerIdle(),
        (M.SERVER_LOADED, var path) => new ServerLoaded { WorkspacePath = path.AsUtf8String() },
        (M.SERVER_RUNNING, var path) => new ServerRunning { WorkspacePath = path.AsUtf8String() },
        (M.DISCONNECTED, _) => new Disconnected(),
        _ => throw new NotSupportedException($"Message: {m.Type}")
    };
}