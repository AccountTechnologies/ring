namespace ATech.Ring.DotNet.Cli.Tools
{
    public class ExecutionInfo
    {
        public int Pid { get; set; }
        public int? ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public bool IsSuccess => ExitCode == 0;
    }
}
