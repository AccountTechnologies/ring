using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using ATech.Ring.Vsix.Annotations;
using ATech.Ring.Vsix.Client.Commands;
using ATech.Ring.Vsix.StateMachines.Solution;
using ATech.Ring.Vsix.StateMachines.Solution.Data;
using ATech.Ring.Vsix.StateMachines.Workspace;
using ATech.Ring.Vsix.StateMachines.Workspace.Data;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using S = ATech.Ring.Vsix.StateMachines.Workspace.WorkspaceState;
using T = ATech.Ring.Vsix.StateMachines.Workspace.WorkspaceTrigger;
using Task = System.Threading.Tasks.Task;
using WorkspaceState = ATech.Ring.Protocol.Events.WorkspaceState;

namespace ATech.Ring.Vsix.ViewModel
{
    public class WorkspaceViewModel : INotifyPropertyChanged
    {
        private readonly SolutionStateMachine _solutionMachine;
        private readonly WorkspaceStateMachine _workspaceFsm;
        private readonly Action<string> _writeLog;
        private readonly ISender<IRingCommand> _commandSender;
        private SubWorkspaceVm _root;
        private RunnablesViewModel _runnables;

        private bool _isOpenButtonEnabled = true;
        private bool _isCloseButtonEnabled;
        private bool _isStartButtonEnabled;
        private bool _isStopButtonEnabled;
        private bool _isSyncButtonEnabled;
        private Visibility _filePathVisibility = Visibility.Collapsed;

        public WorkspaceViewModel(SolutionStateMachine solutionMachine,
                                  WorkspaceStateMachine workspaceMachine,
                                  Action<string> writeLog,
                                  ISender<IRingCommand> commandSender)
        {
            _writeLog = writeLog;
            _commandSender = commandSender;
            _runnables = new RunnablesViewModel(writeLog);
            _solutionMachine = solutionMachine;
            _workspaceFsm = workspaceMachine;
            _root = new SubWorkspaceVm(commandSender, writeLog, new WorkspaceInfo("", new RunnableInfo[0], ServerState.IDLE, WorkspaceState.IDLE));
            Status = new WorkspaceStatusVm();
            Status.None();
            IsSyncButtonEnabled = true;
            IsOpenButtonEnabled = false;
            FilePathVisibility = Visibility.Collapsed;
            Configure(_workspaceFsm);
        }

        private void Configure(WorkspaceStateMachine m)
        {
            m.OnTransitioned(t => _writeLog($"w: {t.Source} -> ({t.Trigger}) -> {t.Destination}"));
            m.OnUnhandledTrigger((s, t) => _writeLog($"w: Cannot trigger {t} in state {s}"));

            m.Configure(S.NotLoaded)
                .OnEntryFrom(T.Unload, OnUnload)
                .OnEntryFrom(T.Detach, OnDetach)
                .OnEntryFromAsync(T.Sync.With<WorkspaceInfoData>(), async data => await OnSyncAsync(data))
                .InternalTransition(T.Load.With<WorkspaceLoadData>(), (data, _) => OnLoad(data))
                .Ignore(T.SolutionLoaded)
                .Ignore(T.ProjectDebugStart)
                .Ignore(T.ProjectDebugStop)
                .Ignore(T.ProjectBuildStart)
                .Ignore(T.ProjectBuildStop)
                .Ignore(T.Start)
                .Ignore(T.Stop)
                .PermitDynamic(T.Sync.With<WorkspaceInfoData>(), GetSyncState);


            m.Configure(S.Idle)
                .SubstateOf(S.Active)
                .OnEntryFromAsync(T.Sync.With<WorkspaceInfoData>(), async data => await OnSyncAsync(data))
                .OnEntryFrom(T.Stop, OnStop)
                .OnEntryFrom(T.Load.With<WorkspaceLoadData>(), OnLoad)
                .OnEntryFrom(T.Error, OnError)
                .PermitDynamic(T.Sync.With<WorkspaceInfoData>(), GetSyncState)
                .Permit(T.Unload, S.NotLoaded)
                .Permit(T.Detach, S.NotLoaded)
                .Permit(T.Start, S.Running);

            m.Configure(S.Running)
                    .SubstateOf(S.Active)
                    .OnEntryFromAsync(T.Sync.With<WorkspaceInfoData>(), async data => await OnSyncAsync(data))
                    .OnEntryFrom(T.Start, OnStart)
                    .PermitDynamic(T.Sync.With<WorkspaceInfoData>(), GetSyncState)
                    .Permit(T.Error, S.Idle)
                    .Permit(T.Stop, S.Idle)
                    .Permit(T.Detach, S.NotLoaded);

            m.Configure(S.Active)
                    .InternalTransition(T.ProjectDebugStart.With<RunnableNames>(), (d, _) => OnProjectDebugStart(d))
                    .InternalTransition(T.ProjectDebugStop.With<RunnableName>(), (d, _) => OnProjectDebugStop(d))
                    .InternalTransition(T.ProjectBuildStart.With<RunnableName>(), (d, _) => OnProjectBuildStart(d))
                    .InternalTransition(T.ProjectBuildStop.With<RunnableName>(), (d, _) => OnProjectBuildStop(d))
                    .InternalTransition(T.SolutionDebugStop, OnSolutionDebugStop)
                    .InternalTransition(T.SolutionLoaded.With<SolutionLoadedData>(), (d, _) => OnSolutionLoaded(d))
                    .InternalTransition(T.SolutionUnloaded, OnSolutionUnloaded)
                    .InternalTransition(T.SolutionBuildStart, OnSolutionBuildStart)
                    .InternalTransition(T.SolutionBuildStop, OnSolutionBuildStop)
                    .Permit(T.Unload, S.NotLoaded)
                    .Permit(T.Stop, S.Idle)
                    .Permit(T.Error, S.Idle);

            static S GetSyncState(WorkspaceInfoData e) =>
                e.Workspace.ServerState switch
                {
                    ServerState.IDLE => S.NotLoaded,
                    ServerState.LOADED => S.Idle,
                    ServerState.RUNNING => S.Running,
                    _ => throw new ArgumentOutOfRangeException(nameof(e.Workspace.ServerState), e.Workspace.ServerState, "State not supported")
                };
        }

        private void OnStart()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _commandSender.StartWorkspace();
            OnStartUi();
        }

        private void OnStartUi()
        {
            Status.Pending();
            IsStartButtonEnabled = false;
            IsStopButtonEnabled = true;
            IsCloseButtonEnabled = false;
        }

        private void OnStop()
        {
            _commandSender.StopWorkspace();
            HandleUi();

            void HandleUi()
            {
                Status.Pending();
                IsStartButtonEnabled = true;
                IsStopButtonEnabled = false;
                IsCloseButtonEnabled = true;
            }
        }

        private void OnError()
        {
            _solutionMachine.Fire(SolutionTrigger.WorkspaceFaulted);
            OnStop();
        }

        private void OnSolutionLoaded(SolutionLoadedData data) => RunnableModel.SetInSolution(data.ProjectNames);
        private void OnSolutionUnloaded() => RunnableModel.UnsetInSolution();
        private void OnSolutionBuildStart() => RunnableModel.SetAllBuilding();
        private void OnSolutionBuildStop() => RunnableModel.UnsetAllBuilding();
        private void OnSolutionDebugStop() { }

        public Visibility FilePathVisibility
        {
            get => _filePathVisibility;
            set
            {
                _filePathVisibility = value;
                OnPropertyChanged();
            }
        }

        public bool IsOpenButtonEnabled
        {
            get => _isOpenButtonEnabled;
            set
            {
                _isOpenButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsCloseButtonEnabled
        {
            get => _isCloseButtonEnabled;
            set
            {
                _isCloseButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsStartButtonEnabled
        {
            get => _isStartButtonEnabled;
            set
            {
                _isStartButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsStopButtonEnabled
        {
            get => _isStopButtonEnabled;
            set
            {
                _isStopButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsSyncButtonEnabled
        {
            get => _isSyncButtonEnabled;
            set
            {
                _isSyncButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        private WorkspaceStatusVm _status;
        public WorkspaceStatusVm Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public string FilePath { get; set; }

        public RunnablesViewModel RunnableModel
        {
            get => _runnables;
            set
            {
                _runnables = value;
                OnPropertyChanged();
            }
        }

        public SubWorkspaceVm Root
        {
            get => _root;
            set
            {
                _root = value;
                OnPropertyChanged();
            }
        }

        private async Task OnSyncAsync(WorkspaceInfoData data)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                FilePath = data.Workspace.Path;
                FilePathVisibility = Visibility.Visible;
                Root = new SubWorkspaceVm(_commandSender, _writeLog, data.Workspace).MapWorkspace(data.Workspace);
                RunnableModel = new RunnablesViewModel(_writeLog).Map(Root);

                switch (data.Workspace.ServerState)
                {
                    case ServerState.IDLE:
                        IsOpenButtonEnabled = true;
                        IsStartButtonEnabled = false;
                        IsStopButtonEnabled = false;
                        IsCloseButtonEnabled = false;
                        break;
                    case ServerState.LOADED:
                        IsOpenButtonEnabled = false;
                        IsStartButtonEnabled = true;
                        IsStopButtonEnabled = false;
                        IsCloseButtonEnabled = true;
                        await _solutionMachine.FireAsync(SolutionTrigger.WorkspaceLoaded.With<RunnablesViewModel>(), RunnableModel);
                        break;
                    case ServerState.RUNNING:
                        IsOpenButtonEnabled = false;
                        IsStartButtonEnabled = false;
                        IsStopButtonEnabled = true;
                        IsCloseButtonEnabled = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                switch (data.Workspace.WorkspaceState)
                {
                    case WorkspaceState.DEGRADED:
                        _status.Degraded();
                        break;
                    case WorkspaceState.HEALTHY:
                        _status.Healthy();
                        break;
                    case WorkspaceState.IDLE:
                        _status.Idle();
                        break;
                    case WorkspaceState.NONE:
                        _status.None();
                        break;
                    default:
                        _status.None();
                        break;
                }

                _writeLog($"Workspace synced: {data.Workspace.Path}");
            }
            catch (Exception ex)
            {
                _writeLog($"Could not sync workspace: {data.Workspace.Path}. Exception: {ex}");
            }
        }

        private void OnLoad(WorkspaceLoadData data)
        {
            _commandSender.LoadWorkspace(data.FilePath);
        }

        private void OnUnload()
        {
            _commandSender.UnloadWorkspace();
            OnDetach();
        }

        private void OnDetach()
        {
            FilePath = null;
            Root = null;
            RunnableModel.Reset();
            _solutionMachine.Fire(SolutionTrigger.WorkspaceUnloaded);
            Status.None();
        }

        private void OnProjectDebugStop(RunnableName data) => RunnableModel.Items.ForEach(r => r.DebugStop(), data.RunnableId);
        private void OnProjectDebugStart(RunnableNames data) => RunnableModel.StopBeforeDebug(data.RunnableIds);
        private void OnProjectBuildStart(RunnableName data) => RunnableModel.Items.ForEach(r => r.BuildStart(), data.RunnableId);
        private void OnProjectBuildStop(RunnableName data) => RunnableModel.Items.ForEach(r => r.BuildStop(), data.RunnableId);

        public Task AttachIdleAsync()
        {
            IsOpenButtonEnabled = true;
            return Task.CompletedTask;
        }

        public async Task AttachLoadedAsync(WorkspaceInfo wsInfo)
        {
            await _workspaceFsm.FireAsync(T.Sync.With<WorkspaceInfoData>(), new WorkspaceInfoData { Workspace = wsInfo });
        }


        public async Task LoadAsync(string filePath) => await _workspaceFsm.FireAsync(T.Load.With<WorkspaceLoadData>(), new WorkspaceLoadData { FilePath = filePath });
        public async Task DetachAsync()
        {
            if (!_workspaceFsm.CanFire(T.Detach)) return;
            await _workspaceFsm.FireAsync(T.Detach);
        }

        public async Task StartWorkspaceAsync() => await _workspaceFsm.FireAsync(T.Start);
        public async Task StopAsync()
        {
            if (!_workspaceFsm.CanFire(T.Stop)) return;
            await _workspaceFsm.FireAsync(T.Stop);
        }

        public async Task UnloadAsync()
        {
            if (!_workspaceFsm.CanFire(T.Unload)) return;
            await _workspaceFsm.FireAsync(T.Unload);
        }

        public Task RequestInfoAsync()
        {
            _commandSender.RequestWorkspaceInfo();
            return Task.CompletedTask;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}