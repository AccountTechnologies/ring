using System.Diagnostics.CodeAnalysis;

namespace ATech.Ring.Protocol.v2.Events;

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public class WorkspaceInfo : IEquatable<WorkspaceInfo>
{
    public WorkspaceInfo(string path, 
        RunnableInfo[] runnables,
        string[] flavours,
        string currentFlavour,
        ServerState serverState,
        WorkspaceState workspaceState)
    {
        Path = path;
        Runnables = runnables;
        Flavours = flavours;
        CurrentFlavour = currentFlavour;
        ServerState = serverState;
        WorkspaceState = workspaceState;
    }

    public string Path { get; }
    public RunnableInfo[] Runnables { get; }
    public ServerState ServerState { get; }
    public WorkspaceState WorkspaceState { get; }
    public string[] Flavours { get; }

    [DisallowNull]
    public string? CurrentFlavour { get; }

    public bool Equals(WorkspaceInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Path, other.Path) && Runnables.SequenceEqual(other.Runnables) && ServerState == other.ServerState && WorkspaceState == other.WorkspaceState;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((WorkspaceInfo)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Path, Runnables, ServerState, WorkspaceState);
    public static WorkspaceInfo Empty { get; } = new(string.Empty, Array.Empty<RunnableInfo>(), Array.Empty<string>(), string.Empty, ServerState.IDLE, WorkspaceState.NONE);
    public ReadOnlySpan<byte> Serialize() => JsonSerializer.SerializeToUtf8Bytes(this, SerializerOptions.Value);

    private static readonly Lazy<JsonSerializerOptions> SerializerOptions = new(() =>
    {
        var options = new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    });
}
