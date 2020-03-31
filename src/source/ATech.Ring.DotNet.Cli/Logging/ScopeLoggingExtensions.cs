using System;
using ATech.Ring.DotNet.Cli.Abstractions;
using Microsoft.Extensions.Logging;

namespace ATech.Ring.DotNet.Cli.Logging
{
    public static class ScopeLoggingExtensions
    {
        internal static Scope ToScope(this IRunnable r) => new Scope { [Scope.UniqueIdKey] = r.UniqueId };
        public static void LogDebug<T>(this ILogger<T> logger, PhaseStatus s) => logger.LogDebug("{Status}", s);
        public static void LogInformation<T>(this ILogger<T> logger, PhaseStatus s) => logger.LogInformation("{Status}", s);
        public static void LogContextDebug<T>(this ILogger<T> logger, object context) => logger.LogDebug("{@Context}", context);
        public static IDisposable WithProtocolScope<T, T2>(this ILogger<T> logger, T2 phase) =>
            logger.BeginScope(new Scope { [Scope.UniqueIdKey] = "PROTOCOL", [Scope.PhaseKey] = phase });
        public static IDisposable WithHostScope<T>(this ILogger<T> logger, Phase phase) => logger.WithScope("HOST", phase);
        public static IDisposable WithScope<T>(this ILogger<T> logger, string runtimeId, Phase phase) => logger.BeginScope(new Scope { [Scope.UniqueIdKey] = runtimeId, [Scope.PhaseKey] = phase });
    }
}