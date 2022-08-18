namespace ATech.Ring.DotNet.Cli.Tools;

public readonly struct ExecutionInfo
{
    public ExecutionInfo(int pid, int? exitCode, string output)
    {
        Pid = pid;
        ExitCode = exitCode;
        Output = output;
    }

    public int Pid { get; }
    public int? ExitCode { get; }
    public string Output { get; }
    public bool IsSuccess => ExitCode == 0;
    public static readonly ExecutionInfo Empty = new(0, 0, string.Empty);
}