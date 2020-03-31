using System;
using System.Linq;

namespace ATech.Ring.Protocol.Events
{
    public class WorkspaceInfo : IEquatable<WorkspaceInfo>
    {
        public WorkspaceInfo(string path, RunnableInfo[] runnables, ServerState serverState, WorkspaceState workspaceState)
        {
            Path = path;
            Runnables = runnables;
            ServerState = serverState;
            WorkspaceState = workspaceState;
        }

        public string Path { get;  }
        public RunnableInfo[] Runnables { get;  }
        public ServerState ServerState { get;  }
        public WorkspaceState WorkspaceState { get;  }

        public bool Equals(WorkspaceInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Path, other.Path) && Runnables.SequenceEqual(other.Runnables) && ServerState == other.ServerState && WorkspaceState == other.WorkspaceState;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WorkspaceInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Runnables != null ? Runnables.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) ServerState;
                hashCode = (hashCode * 397) ^ (int) WorkspaceState;
                return hashCode;
            }
        }
    }
}