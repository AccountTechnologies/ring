namespace ATech.Ring.Protocol.v2;

using ATech.Ring.Protocol.v2.Events;
using System;
using System.Text;

public enum M : byte
{
    EMPTY = 255,
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
    SERVER_RUNNING = 24,
    SERVER_SHUTDOWN = 25
}

public interface IAsMessage
{
    Message AsMessage();
    M Type { get; }
}

public static class ReadOnlySpanExtensions
{
    public static string AsUtf8String(this ReadOnlySpan<byte> span) => Encoding.UTF8.GetString(span);
    public static ReadOnlySpan<byte> SliceUntilNull(this ReadOnlySpan<byte> span)
    {
        var nullChar = span.IndexOf((byte)0);
        return span[..(nullChar == -1 ? span.Length : nullChar)];
    }
}

public readonly ref struct Message
{
    public ReadOnlySpan<byte> Bytes { get; }
    public M Type => Bytes.Length == 0 ? M.EMPTY : (M)Bytes[0];
    public ReadOnlySpan<byte> Payload => Bytes.Length <= 1 ? ReadOnlySpan<byte>.Empty : Bytes[1..].SliceUntilNull();
    public string PayloadString => Payload.AsUtf8String();

    public Message(ReadOnlySpan<byte> bytes) => Bytes = bytes.SliceUntilNull();
    public Message(M type, string value) : this(type, Encoding.UTF8.GetBytes(value)) { }
    public Message(M type, byte value) : this(type, new byte[] {value}) { }
    public Message(M type, ReadOnlySpan<byte> bytes)
    {
        var trimmedBytes = bytes.SliceUntilNull();
        byte[] newBytes = new byte[trimmedBytes.Length + 1];
        newBytes[0] = (byte)type;
        Array.Copy(trimmedBytes.ToArray(), 0, newBytes, 1, trimmedBytes.Length);
        Bytes = newBytes;
    }

    public Message(M type, ReadOnlySpan<char> chars)
    {
        byte[] newBytes = new byte[chars.Length + 1];
        newBytes[0] = (byte)type;
        Encoding.UTF8.GetBytes(chars, newBytes.AsSpan()[1..]);
        Bytes = new ReadOnlySpan<byte>(newBytes).SliceUntilNull();
    }

    public Message(M type) => Bytes = new byte[] { (byte)type };

    public void Deconstruct(out M type, out ReadOnlySpan<byte> payload)
    {
        type = Type;
        payload = Payload;
    }

    public override string ToString()
    {
        var (type, payload) = this;
        var maybeColon = payload == ReadOnlySpan<byte>.Empty ? "" : ":";
        return $"{type}{maybeColon}{payload.AsUtf8String()}";
    }

    public static implicit operator ReadOnlySpan<byte>(Message m) => m.Bytes;
    public static implicit operator Message(ReadOnlySpan<byte> bytes) => new(bytes);
    public static implicit operator Message(M type) => new(type);
    public static Message RunnableInitiated(ReadOnlySpan<char> id) => new(M.RUNNABLE_INITIATED, id);
    public static Message RunnableStarted(ReadOnlySpan<char> id) => new(M.RUNNABLE_STARTED, id);
    public static Message RunnableHealthCheck(ReadOnlySpan<char> id) => new(M.RUNNABLE_HEALTH_CHECK, id);
    public static Message RunnableHealthy(ReadOnlySpan<char> id) => new(M.RUNNABLE_HEALTHY, id);
    public static Message RunnableDead(ReadOnlySpan<char> id) => new(M.RUNNABLE_UNRECOVERABLE, id);
    public static Message RunnableRecovering(ReadOnlySpan<char> id) => new(M.RUNNABLE_RECOVERING, id);
    public static Message RunnableStopped(ReadOnlySpan<char> id) => new(M.RUNNABLE_STOPPED, id);
    public static Message RunnableDestroyed(ReadOnlySpan<char> id) => new(M.RUNNABLE_DESTROYED, id);
    public static Message ServerIdle() => new(M.SERVER_IDLE);
    public static Message ServerLoaded(ReadOnlySpan<char> workspacePath) => new(M.SERVER_LOADED, workspacePath);
    public static Message ServerRunning(ReadOnlySpan<char> workspacePath) => new(M.SERVER_RUNNING, workspacePath);
    public static Message WorkspaceInfo(WorkspaceInfo info) => new(M.WORKSPACE_INFO_PUBLISH, info.Serialize());
    public static Message Empty() => new(M.EMPTY);
}
