using ATech.Ring.Protocol.Events;

namespace ATech.Ring.Vsix.Client
{
    public static class RunnableInfoExtensions
    {
        public static bool TryGetCsProjPath(this RunnableInfo r, out string csProjPath)
        {
            csProjPath = string.Empty;
            if (r.Details == null || !r.Details.ContainsKey(DetailsKeys.CsProjPath)) return false;
            csProjPath = r.Details[DetailsKeys.CsProjPath] as string;
            return true;
        }
    }
}