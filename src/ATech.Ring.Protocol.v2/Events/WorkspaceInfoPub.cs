using System;

namespace ATech.Ring.Protocol.v2.Events;

public sealed class WorkspaceInfoPub : IRingEvent
{
    public M Type => M.WORKSPACE_INFO_PUBLISH;
    public WorkspaceInfo WorkspaceInfoJson { get; }
    public WorkspaceInfoPub(WorkspaceInfo info) => WorkspaceInfoJson = info;
    public WorkspaceInfoPub(ReadOnlySpan<byte> info) => WorkspaceInfoJson = WorkspaceInfo.Deserialize(info);
    public Message AsMessage() => Message.FromUtf8Bytes(Type, WorkspaceInfoJson.Serialize());
}
