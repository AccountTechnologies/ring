using System.Collections.Generic;

namespace ATech.Ring.DotNet.Cli.Logging
{
    public class Scope : Dictionary<string, object>
    {
        public const string PhaseKey = "Phase";
        public const string UniqueIdKey = "UniqueId";
        public static Scope Phase(Phase phase) => new Scope { { PhaseKey, phase } };
    }
}