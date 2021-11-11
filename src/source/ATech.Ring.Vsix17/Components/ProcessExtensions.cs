using System;
using System.Diagnostics;
using System.Linq;

namespace ATech.Ring.Vsix.Components
{
    internal static class ProcessExtensions
    {
        internal static void TerminateIISExpress(Action<string> writeLog)
        {
            const string iisExpressProcessName = "iisexpress";
            var iisExpressProcesses = Process.GetProcesses().Where(x => x.ProcessName.Equals(iisExpressProcessName));
            foreach (var p in iisExpressProcesses) p.KillSafe(writeLog);
        }

        public static void KillSafe(this Process p, Action<string> writeLog)
        {
            try
            {
                var pid = p.Id;
                p.Kill();
                writeLog($"Process killed ({pid})");
            }
            catch (Exception ex)
            {
                writeLog($"{nameof(KillSafe)}. Exception: {ex}");
            }
        }
    }
}