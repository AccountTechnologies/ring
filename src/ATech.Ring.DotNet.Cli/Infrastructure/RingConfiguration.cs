using System;

namespace ATech.Ring.DotNet.Cli.Infrastructure
{
    public class RingConfiguration
    {
        public string GitCloneRootPath { get; set; }
        public string KustomizeCacheRootPath { get; set; }
        public HooksConfiguration Hooks { get; set; }
    }

    public class InitHookConfig
    {
        public string Command { get; set; }
        public string[] Args { get; set; } = Array.Empty<string>();
    }

    public class HooksConfiguration
    {
        public InitHookConfig Init { get; set; }
    }
}