namespace ATech.Ring.DotNet.Cli.Windows.Tools
{
    public class ExecutionInfo
    {
        public int Pid { get; set; }
        public int? ExitCode { get; set; }
        public string Output { get; set; }
        public bool IsSuccess => ExitCode == 0;
    }
}