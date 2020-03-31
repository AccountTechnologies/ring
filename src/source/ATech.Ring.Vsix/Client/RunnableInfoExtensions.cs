using ATech.Ring.Protocol.Events;

namespace ATech.Ring.Vsix.Client
{
    public static class RunnableInfoExtensions
    {
        public static bool TryGetCsProjPath(this RunnableInfo r, out string csProjPath)
        {
            const string csProjPathKey = "csProjPath";
            csProjPath = string.Empty;
            if (r.Details == null || !r.Details.ContainsKey(csProjPathKey)) return false;
            csProjPath = r.Details[csProjPathKey] as string;
            return true;
        }
    }
}