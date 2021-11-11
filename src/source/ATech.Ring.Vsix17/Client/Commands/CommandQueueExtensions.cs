using ATech.Ring.Protocol;
using ATech.Ring.Vsix.ViewModel;

namespace ATech.Ring.Vsix.Client.Commands
{
    public static class CommandQueueExtensions
    {
        public static void Include(this ISender<IRingCommand> q, RunnableVm r)
            => q.Enqueue(new RunnableInclude {UniqueId = r.UniqueId });

        public static void Exclude(this ISender<IRingCommand> q, RunnableVm r)
            => q.Enqueue(new RunnableExclude { UniqueId = r.UniqueId });

        public static void AllRunnablesUp(this ISender<IRingCommand> q) => q.Enqueue(new AllRunnablesUp());
        public static void AllRunnablesDown(this ISender<IRingCommand> q) => q.Enqueue(new AllRunnablesDown());
        public static void LoadWorkspace(this ISender<IRingCommand> q, string fullPath) => q.Enqueue(new LoadWorkspace {FullPath = fullPath});
        public static void RequestWorkspaceInfo(this ISender<IRingCommand> q) => q.Enqueue(new RequestWorkspaceInfo());
        public static void UnloadWorkspace(this ISender<IRingCommand> q) => q.Enqueue(new UnloadWorkspace());
        public static void StartWorkspace(this ISender<IRingCommand> q) => q.Enqueue(new StartWorkspace());
        public static void StopWorkspace(this ISender<IRingCommand> q) => q.Enqueue(new StopWorkspace());
        public static void Terminate(this ISender<IRingCommand> q) => q.Enqueue(new Terminate());
        public static void Ping(this ISender<IRingCommand> q) => q.Enqueue(new Ping());
    }
}