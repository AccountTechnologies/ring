using ATech.Ring.Protocol.Events;
using System;
using System.Text;
using Newtonsoft.Json;

namespace ATech.Ring.Protocol
{
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
    public class Message
    {
        private ReadOnlyMemory<byte> Bytes { get; }

        public Message(ReadOnlyMemory<byte> bytes) => Bytes = bytes;

        public void Deconstruct(out M type, out string payload)
        {
            type = (M)Bytes.Span[0];
            payload = Bytes.Length == 1 ? string.Empty : AsString(SliceUntilNull(Bytes.Slice(1)));
        }

        public override string ToString()
        {
            var (type, payload) = this;
            var maybeColon = string.IsNullOrWhiteSpace(payload) ? "" : ":";
            return $"{type}{maybeColon}{payload}";
        }

        private static ReadOnlySpan<byte> SliceUntilNull(ReadOnlyMemory<byte> m)
        {
            var nullChar = m.Span.IndexOf((byte)0);
            return m.Slice(0, nullChar == -1 ? m.Length : nullChar).Span;
        }

        private static string AsString(ReadOnlySpan<byte> span) => Encoding.UTF8.GetString(span.ToArray());

        public static implicit operator ReadOnlyMemory<byte>(Message m) => m.Bytes;
        public static implicit operator Message(Memory<byte> bytes) => Create(bytes);

        public static Message Empty = Memory<byte>.Empty;
        public static Message From(M messageType) => Create(new[] { (byte)messageType }.AsMemory());
        public static Message FromString(M messageType, string payload)
        {
            var array = Encoding.UTF8.GetBytes(0 + payload);
            array[0] = (byte)messageType;
            return Create(array.AsMemory());
        }
        private static Message Create(ReadOnlyMemory<byte> bytes) => new Message(bytes);
    }

    public static class FromMessage
    {
        public static IRingEvent AsEvent(this Message m) => m switch
        {
            (M.ACK, var ack) => new AckEvent(ack),
            (M.RUNNABLE_INITIATED, var runnableId) => new RunnableInitiated { UniqueId = runnableId },
            (M.RUNNABLE_STARTED, var runnableId) => new RunnableStarted { UniqueId = runnableId },
            (M.RUNNABLE_UNRECOVERABLE, var runnableId) => new RunnableDead { UniqueId = runnableId },
            (M.RUNNABLE_HEALTH_CHECK, var runnableId) => new RunnableHealthCheck { UniqueId = runnableId },
            (M.RUNNABLE_HEALTHY, var runnableId) => new RunnableHealthy { UniqueId = runnableId },
            (M.RUNNABLE_RECOVERING, var runnableId) => new RunnableRecovering { UniqueId = runnableId },
            (M.RUNNABLE_STOPPED, var runnableId) => new RunnableStopped { UniqueId = runnableId },
            (M.RUNNABLE_DESTROYED, var runnableId) => new RunnableDestroyed { UniqueId = runnableId },
            (M.WORKSPACE_INFO_PUBLISH, var data) => new WorkspaceInfoPub { WorkspaceInfoJson = JsonConvert.DeserializeObject<WorkspaceInfo>(data) },
            (M.SERVER_IDLE, _) => new ServerIdle(),
            (M.SERVER_LOADED, var path) => new ServerLoaded { WorkspacePath = path},
            (M.SERVER_RUNNING, var path) => new ServerRunning { WorkspacePath = path},
            (M.DISCONNECTED,_) => new Disconnected(),
            _ => throw new NotSupportedException($"Message: {m}")
        };
    }
}