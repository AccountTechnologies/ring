using System;
using System.Collections.Generic;
using System.Linq;

namespace ATech.Ring.Protocol.Events
{
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

        public string Id { get;  }
        public string[] DeclaredIn { get;  }
        public string Type { get;  }
        public RunnableState State { get; }
        public IReadOnlyDictionary<string, object> Details { get; }

        public bool Equals(RunnableInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Id, other.Id) && DeclaredIn.SequenceEqual(other.DeclaredIn) && string.Equals(Type, other.Type) && State == other.State && Details.SequenceEqual(other.Details);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RunnableInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DeclaredIn != null ? DeclaredIn.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) State;
                hashCode = (hashCode * 397) ^ (Details != null ? Details.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}