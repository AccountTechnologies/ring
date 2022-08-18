using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ATech.Ring.DotNet.Cli.Abstractions;

public interface IRunnable
{
    Task RunAsync(CancellationToken token);
    Task TerminateAsync();
    string UniqueId { get; }
    State State { get; }
    event EventHandler OnHealthCheckCompleted;
    event EventHandler OnInitExecuted;
    IReadOnlyDictionary<string,object> Details { get; }
}