using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using ATech.Ring.Vsix.Client;
using ATech.Ring.Vsix.Interfaces;
using ATech.Ring.Vsix.StateMachines.Solution;
using ATech.Ring.Vsix.StateMachines.Workspace;
using ATech.Ring.Vsix.ViewModel;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ATech.Ring.Vsix.Client.Commands;
using Newtonsoft.Json.Linq;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RunnableVm = ATech.Ring.Vsix.ViewModel.RunnableVm;
using Task = System.Threading.Tasks.Task;

namespace ATech.Ring.Vsix
{
    public partial class RingWindowControl : UserControl, IDisposable
    {
        private readonly WorkspaceViewModel _workspace;
        private static readonly Guid UniqueRingOutputId = new Guid("ccb09ee6-f62b-4848-bc97-a145d91db91b");
        private const string RingOutputTitle = "ring!";
        //TODO: think of a way to have the port read from some settings
        private readonly string _ringServerUri = "ws://localhost:7999/ws";
        private readonly RingManager _ringManager;
        private CancellationTokenSource _cts;
        public RingWindowControl()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var workspaceMachine = new WorkspaceStateMachine();
            var solutionMachine = new SolutionStateMachine();
            var commandQueue = new Queue<IRingCommand>();
            
            var ringClient = new RingClient(new Uri(_ringServerUri), commandQueue, OutputWriteLine);
            var solution = new SolutionViewModel(solutionMachine, workspaceMachine, OutputWriteLine, commandQueue);
            _workspace = new WorkspaceViewModel(solutionMachine, workspaceMachine, OutputWriteLine, commandQueue);
            _ringManager = new RingManager(ringClient, OutputWriteLine);
            DataContext = new RingWindowViewModel(solutionMachine, workspaceMachine, solution, _workspace);
            InitializeComponent();
        }

        public async Task InitializeAsync(GetServiceAsync getServiceAsync, CancellationToken token)
        {
            _cts ??= CancellationTokenSource.CreateLinkedTokenSource(token);
            await _ringManager.EnsureServerAsync(token);
        }

        private static IVsOutputWindowPane CreateRingOutputWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(Package.GetGlobalService(typeof(SVsOutputWindow)) is IVsOutputWindow outWindow)) throw new InvalidOperationException($"{nameof(outWindow)} should not be null");
            var customGuid = UniqueRingOutputId;
            outWindow.CreatePane(ref customGuid, RingOutputTitle, 1, 1);
            outWindow.GetPane(ref customGuid, out var customPane);
            return customPane;
        }

        private static readonly IVsOutputWindowPane RingOutPane = CreateRingOutputWindow();

        private static void OutputWriteLine(string message)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                RingOutPane.OutputStringThreadSafe($"\r\n[ring!vsix] {message}");
            });
        }

        private void OpenButton_OnClick(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var ofd = new OpenFileDialog { Filter = "ring! workspaces (*.toml) | *.toml" };
                var result = ofd.ShowDialog();
                if (result != true) return;
                await _workspace.LoadAsync(ofd.FileName);
            });
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _workspace.StartWorkspaceAsync();
            });
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _workspace.StopAsync();
            });
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _workspace.UnloadAsync();
            });
        }

        private void RunnableAction_Click(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            var runnable = (RunnableVm)b.DataContext;
            runnable.Action();
        }

        private async Task DispatchAsync(IRingEvent @event)
        {
            var _ = @event switch
            {
                RunnableHealthCheck r => Runnable(r, r => r.HealthCheck()),
                RunnableHealthy r => Runnable(r, r => r.Healthy()),
                RunnableDead r => Runnable(r, r => r.Dead()),
                RunnableInitiated r => Runnable(r, r => r.Initiated()),
                RunnableStarted r => Runnable(r, r => r.Started()),
                RunnableStopped r => Runnable(r, r => r.Stopped()),
                RunnableRecovering r => Runnable(r, r => r.Recovering()),
                RunnableDestroyed r => Runnable(r, r => r.Destroyed()),
                WorkspaceInfoPub data => await Workspace(w => w.AttachLoadedAsync(data.WorkspaceInfoJson)), 
                ServerIdle _ => await Workspace(w => w.AttachIdleAsync()),
                ServerLoaded { WorkspacePath: string path } => await Workspace(w => w.RequestInfoAsync()),
                ServerRunning { WorkspacePath: string path } => await Workspace(w => w.RequestInfoAsync()),
                AckEvent _ => true,
                Disconnected _  => true,
                _ => Log()
            };

            bool Log()
            {
                OutputWriteLine($"Not supported event type {@event.GetType()}");
                return true;
            }

            async Task<bool> Workspace(Func<WorkspaceViewModel, Task> action)
            {
                await action(_workspace);
                return true;
            }

            bool Runnable(RunnableEvent r, Action<RunnableVm> action)
            {
                _workspace.RunnableModel.Items.ForEach(action, r.UniqueId);
                return true;
            }
        }

        public async Task DestroyAsync() => await _ringManager.DestroyAsync();
        public void Dispose() => _ringManager?.Dispose();

        private void SyncServer_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                await _ringManager.DisconnectAsync();
                await _workspace.DetachAsync();
                await _ringManager.ConnectAsync(DispatchAsync, _cts.Token);
            });
        }

        private static string GetDetail(object sender, string detailKey)
        {
            var mi = (MenuItem)sender;
            var r = (RunnableVm)mi.DataContext;
            var detail = r.Model.Details[detailKey];
            if (detail is JArray arr)
            {
                return arr.First.Value<string>();
            }
            return (string)detail;
        }

        private void RevealInOctant_OnClick(object sender, RoutedEventArgs e)
        {
            var namespacedPods = GetDetail(sender, DetailsKeys.KubernetesPods).Split('|')[0];
            var chunks = namespacedPods.Split('/');
            var ns = chunks[0];
            var pod = chunks[1];

            Process.Start($"http://127.0.0.1:7777/#/overview/namespace/{ns}/workloads/pods/{pod}");
        }

        private void BrowseUri_OnClick(object sender, RoutedEventArgs e)
        {
            var uri = GetDetail(sender, DetailsKeys.Uri);
            Process.Start(uri);
        }

        private void OpenFolder_OnClick(object sender, RoutedEventArgs e)
        {
            var workDir = GetDetail(sender, DetailsKeys.WorkDir);
            Process.Start(workDir);
        }
    }
}
