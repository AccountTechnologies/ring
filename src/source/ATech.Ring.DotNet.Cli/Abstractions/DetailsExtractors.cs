using System.Linq;
using ATech.Ring.Configuration.Interfaces;
using ATech.Ring.Configuration.Runnables;
using ATech.Ring.DotNet.Cli.Dtos;

namespace ATech.Ring.DotNet.Cli.Abstractions
{
    public static class DetailsExtractors
    {
        public static RunnableDetails Extract(IRunnableConfig cfg)
        {
            static RunnableDetails New(params (string key, object value)[] details) => new RunnableDetails(details.ToDictionary(x => x.key, x => x.value));
            return cfg switch
                {
                CsProjRunnable c => New((DetailsKeys.CsProjPath, c.FullPath)),
                _ => RunnableDetails.Empty
            };
        }
    }
}