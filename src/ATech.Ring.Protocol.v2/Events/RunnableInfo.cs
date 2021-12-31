namespace ATech.Ring.Protocol.v2.Events;

using System;
using System.Collections.Generic;
using System.Linq;

public class RunnableInfo : IEquatable<RunnableInfo>
{
    public RunnableInfo(string id, string[] declaredIn, string type, RunnableState state, IReadOnlyDictionary<string, object> details)
    {
        Id = id;
        DeclaredIn = declaredIn;
        Type = type;
        State = state;
        Details = details;
    }

    public string Id { get; }
    public string[] DeclaredIn { get; }
    public string Type { get; }
    public RunnableState State { get; }
    public IReadOnlyDictionary<string, object> Details { get; }

    public bool Equals(RunnableInfo other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Id, other.Id) && DeclaredIn.SequenceEqual(other.DeclaredIn) && string.Equals(Type, other.Type) && State == other.State && Details.SequenceEqual(other.Details);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((RunnableInfo)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Id, DeclaredIn, Type, State, Details);
}
