namespace ATech.Ring.Protocol.v2;

using ATech.Ring.Protocol.v2.Events;
using System;
using System.Text;
using System.Text.Json;

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

public static class Eeee
{
    public static string AsUtf8String(this ReadOnlySpan<byte> span) => Encoding.UTF8.GetString(span);
}

public class Message
{
    private ReadOnlyMemory<byte> Bytes { get; }

    public Message(ReadOnlyMemory<byte> bytes) => Bytes = bytes;

    public void Deconstruct(out M type, out ReadOnlySpan<byte> payload)
    {
        type = (M)Bytes.Span[0];
        payload = Bytes.Length == 1 ? ReadOnlySpan<byte>.Empty : SliceUntilNull(Bytes[1..]);
    }

    public override string ToString()
    {
        var (type, payload) = this;
        var maybeColon = payload == ReadOnlySpan<byte>.Empty ? "" : ":";
        return $"{type}{maybeColon}{payload.AsUtf8String()}";
    }

    private static ReadOnlySpan<byte> SliceUntilNull(ReadOnlyMemory<byte> m)
    {
        var nullChar = m.Span.IndexOf((byte)0);
        return m[..(nullChar == -1 ? m.Length : nullChar)].Span;
    }


    public static implicit operator ReadOnlyMemory<byte>(Message m) => m.Bytes;
    public static implicit operator Message(Memory<byte> bytes) => Create(bytes);

    public static Message Empty = Memory<byte>.Empty;
    public static Message From(M messageType) => Create(new[] { (byte)messageType }.AsMemory());
    public static Message FromUtf8Bytes(M messageType, ReadOnlySpan<byte> utf8Bytes)
    {
        throw new NotImplementedException(nameof(FromUtf8Bytes));
    }
    public static Message FromString(M messageType, string payload)
    {
        var array = Encoding.UTF8.GetBytes(0 + payload);
        array[0] = (byte)messageType;
        return Create(array.AsMemory());
    }
    private static Message Create(ReadOnlyMemory<byte> bytes) => new(bytes);
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
        _ => throw new NotSupportedException($"Message: {m}")
    };
}