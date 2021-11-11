using ATech.Ring.Vsix.Components;
using ATech.Ring.Vsix.StateMachines.Solution;
using ATech.Ring.Vsix.StateMachines.Solution.Data;
using ATech.Ring.Vsix.StateMachines.Workspace;
using ATech.Ring.Vsix.StateMachines.Workspace.Data;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using S = ATech.Ring.Vsix.StateMachines.Solution.SolutionState;
using T = ATech.Ring.Vsix.StateMachines.Solution.SolutionTrigger;
using ATech.Ring.Protocol;
using ATech.Ring.Vsix.Client.Commands;
using Task = System.Threading.Tasks.Task;

namespace ATech.Ring.Vsix.ViewModel
{
    public class SolutionViewModel : IDisposable
    {
        private readonly SolutionStateMachine _slnMachine;
        private readonly WorkspaceStateMachine _wsMachine;
        private readonly Action<string> _writeLog;
        private readonly ISender<IRingCommand> _commandQueue;
        private readonly SolutionsEventsHandler _solutionEventsHandler;
        private readonly CommandEvents _projStartDebugEvent;
        private readonly CommandEvents _projStepIntoEvent;
        private readonly CommandEvents _debugStartEvent;
        private readonly DebuggerEvents _debuggerEvents;
        private readonly BuildEvents _buildEvents;
        private readonly DebuggerEventsHandler _debuggerEventsHandler;
        private readonly ProjectsProcesses _projectProcesses;
        private static DTE Dte => VsServicesExtensions.GetGlobalService<DTE>();
        private static IVsSolution Sln => VsServicesExtensions.GetGlobalService<IVsSolution>();
        private static IVsDebugger Debugger => VsServicesExtensions.GetGlobalService<IVsDebugger>();

        public SolutionViewModel(SolutionStateMachine slnMachine,
                                 WorkspaceStateMachine wsMachine,
                                 Action<string> writeLog,
                                 ISender<IRingCommand> commandQueue)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _writeLog = writeLog;
            _commandQueue = commandQueue;
            _wsMachine = wsMachine;
            _slnMachine = slnMachine;
            Configure(_slnMachine);
            var debugStartCmd = Dte.Commands.Item("Debug.Start");

            _solutionEventsHandler = new SolutionsEventsHandler(Sln);
            _debuggerEventsHandler = new DebuggerEventsHandler(Debugger);
            _projStartDebugEvent = Dte.Events.CommandEvents["{" + VSConstants.VSStd2K + "}", (int)VSConstants.VSStd2KCmdID.PROJSTARTDEBUG];
            _projStepIntoEvent = Dte.Events.CommandEvents["{" + VSConstants.VSStd2K + "}", (int)VSConstants.VSStd2KCmdID.PROJSTEPINTO];
            _debugStartEvent = Dte.Events.CommandEvents[debugStartCmd.Guid, debugStartCmd.ID];
            _debuggerEvents = Dte.Events.DebuggerEvents;
            _buildEvents = Dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += BuildBegin;
            _buildEvents.OnBuildDone += BuildDone;
            _buildEvents.OnBuildProjConfigBegin += ProjectBuildBegin;
            _buildEvents.OnBuildProjConfigDone += ProjectBuildDone;
            _projStepIntoEvent.BeforeExecute += DebugProjectsAdd;
            _projStartDebugEvent.BeforeExecute += DebugProjectsAdd;
            _debugStartEvent.BeforeExecute += DebugStart;
            _debuggerEvents.OnEnterDesignMode += DebugStop;
            _solutionEventsHandler.OnAfterOpen += Load;
            _solutionEventsHandler.OnAfterClose += Unload;
            _debuggerEventsHandler.OnProcessCreated += DebugProcessAdd;
            _debuggerEventsHandler.OnProcessRemoved += DebugRemove;
            _projectProcesses = new ProjectsProcesses(writeLog);

            //TODO: handle debug detach (probably kill detached the process - figure out how to get it - potentially get PID on creation)
            //TODO: handle clean up - what should happen if a project or whole solution is cleaned up? Should ring run in a degraded state (With "big exclamation marks" in the window)
        }

        private void Configure(SolutionStateMachine m)
        {
            m.OnTransitioned(t => _writeLog($"s: {t.Source} -> ({t.Trigger}) -> {t.Destination}"));
            m.OnUnhandledTrigger((s, t) => _writeLog($"s: Cannot trigger {t} in state {s}"));

            m.Configure(S.NotLoaded)
                .OnEntryFrom(T.Unload, OnUnload)
                .OnActivate(SolutionAlreadyLoadedCheck)
                .Permit(T.Load, S.Idle)
                .Ignore(T.WorkspaceFaulted)
                .Ignore(T.WorkspaceLoaded)
                .Ignore(T.WorkspaceUnloaded);

            m.Configure(S.Idle)
                .OnEntryFrom(T.Load, OnLoad)
                .OnEntryFrom(T.BuildStop, OnBuildStop)
                .OnEntryFrom(T.DebugStop, OnDebugStop)
                .OnEntryFrom(T.WorkspaceFaulted, OnWorkspaceFaulted)
                .InternalTransition(T.WorkspaceLoaded.With<RunnablesViewModel>(), (a, _) => OnWorkspaceLoaded(a))
                .InternalTransition(T.WorkspaceUnloaded, _ => OnWorkspaceUnloaded())
                .Permit(T.Unload, S.NotLoaded)
                .Permit(T.BuildStart, S.Building)
                .Permit(T.DebugStart, S.Debugging)
                .Permit(T.DebugAdd, S.Debugging);

            m.Configure(S.Building)
                .OnEntryFrom(T.BuildStart, OnBuildStart)
                .Permit(T.WorkspaceFaulted, S.Idle)
                .Permit(T.BuildStop, S.Idle);

            m.Configure(S.Debugging)
                .OnEntryFrom(T.DebugStart, OnDebugStart)
                .OnEntryFromAsync(T.DebugAdd.With<DebugAddData>(), OnDebugAddAsync)
                .OnEntryFromAsync(T.DebugRemove.With<DebugRemoveData>(), OnDebugRemoveAsync)
                .Permit(T.DebugStop, S.Idle)
                .Permit(T.WorkspaceFaulted, S.Idle)
                .PermitReentry(T.DebugAdd)
                .PermitReentry(T.DebugRemove);
        }

        private string[] ProjectNames()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Sln.GetProjectFilesInSolution(0, 0, null, out var projectCount);
            var projectNames = new string[projectCount];
            Sln.GetProjectFilesInSolution(0, projectCount, projectNames, out _);
            return projectNames;
        }

        private void SolutionAlreadyLoadedCheck()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Dte.Solution.IsOpen) Fire(T.Load);
        }

        private void OnLoad()
        {
            FireWs(WorkspaceTrigger.SolutionLoaded, new SolutionLoadedData { ProjectNames = ProjectNames() });
        }

        private void OnUnload()
        {
            FireWs(WorkspaceTrigger.SolutionUnloaded);
        }

        private void OnDebugStart()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(Dte.Solution.SolutionBuild.StartupProjects is IEnumerable startupProjects))
                throw new InvalidOperationException("Debugger started without startup projects?");

            FireWs(WorkspaceTrigger.Start);
            FireRunnableOff(from p in Dte.Solution.AllProjects() where startupProjects.Cast<string>().Contains(p.UniqueName) select p.Name);

        }

        private async Task OnDebugAddAsync(DebugAddData data)
        {
            await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _projectProcesses.AddProcessGuid(data.ProcessId);
            });
        }

        private async Task OnDebugRemoveAsync(DebugRemoveData data)
        {
            await ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var projectName = _projectProcesses.RemoveProjectByProcessId(data.ProcessId);
                await _wsMachine.FireAsync(WorkspaceTrigger.ProjectDebugStop.With<RunnableName>(), new RunnableName { RunnableId = projectName });
            });
        }

        private void FireRunnableOff(IEnumerable<string> projectNames)
        {
            var names = projectNames.ToArray();
            FireWs(WorkspaceTrigger.ProjectDebugStart, new RunnableNames { RunnableIds = names });
            foreach (var name in names) _projectProcesses.AddProject(name);
        }

        private void OnDebugStop()
        {
            _wsMachine.Fire(WorkspaceTrigger.SolutionDebugStop);
        }

        private void OnBuildStart()
        {
            _wsMachine.Fire(WorkspaceTrigger.SolutionBuildStart);
        }

        private void OnBuildStop()
        {
            _wsMachine.Fire(WorkspaceTrigger.SolutionBuildStop);
        }

        private void OnWorkspaceLoaded(RunnablesViewModel model)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            model.SetInSolution(ProjectNames());
            foreach (var p in Dte.Solution.AllProjects()) p.SetStartWebServerOnDebug(false);
        }

        private void OnWorkspaceUnloaded()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var p in Dte.Solution.AllProjects()) p.SetStartWebServerOnDebug(true);
        }

        private void OnWorkspaceFaulted()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode) Dte.Debugger.TerminateAll();
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _solutionEventsHandler.Dispose();
            _debuggerEventsHandler.Dispose();
            _projStepIntoEvent.BeforeExecute -= DebugProjectsAdd;
            _projStartDebugEvent.BeforeExecute -= DebugProjectsAdd;
            _debugStartEvent.BeforeExecute -= DebugStart;
            _solutionEventsHandler.OnAfterOpen -= Load;
            _solutionEventsHandler.OnAfterClose -= Unload;
            _buildEvents.OnBuildProjConfigBegin -= ProjectBuildBegin;
            _buildEvents.OnBuildProjConfigDone -= ProjectBuildDone;
            _debuggerEventsHandler.OnProcessCreated -= DebugProcessAdd;
            _debuggerEventsHandler.OnProcessRemoved -= DebugRemove;
            // OnEnterDesignMode happens when debugger stops. Please note debugged processes might be still alive at this momment so OnProcessRemoved often happens
            // once debugger entered the design mode.
            _debuggerEvents.OnEnterDesignMode -= DebugStop;
        }

        private void DebugProcessAdd(object sender, Guid processId) => Fire(T.DebugAdd, new DebugAddData { ProcessId = processId });
        private void DebugRemove(object sender, Guid processId) => Fire(T.DebugRemove, new DebugRemoveData { ProcessId = processId });
        private void Load(object sender, EventArgs e) => Fire(T.Load);
        private void Unload(object sender, EventArgs e) => Fire(T.Unload);

        /// <summary>
        ///  This handler runs before debug if debug is triggerred as "right click" on a project in solution explorer and "Start new instance" or
        /// "Step into new instance" is chosen. This context guarantees the Active Solution Projects will always contain the project
        /// the command is executed for. 
        /// </summary>
        private void DebugProjectsAdd(string guid, int id, object customIn, object customOut, ref bool cancelDefault)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (!(Dte.ActiveSolutionProjects is IEnumerable activeProjects))
                    throw new InvalidOperationException("Project debug started without active project selected?");

                await _wsMachine.FireAsync(WorkspaceTrigger.Start);
                FireRunnableOff(from p in activeProjects.Cast<Project>() select p.Name);
            });
        }

        /// <summary>
        /// This handler runs before debug if debug is trigerred as F5 or "Play" button in te debug tool bar. In this case the expected behaviour
        /// is that all the startup projects will run (usually it's just a single one)
        /// </summary>
        private void DebugStart(string guid, int id, object customIn, object customOut, ref bool cancelDefault) => Fire(T.DebugStart);

        private void DebugStop(dbgEventReason reason) => Fire(T.DebugStop);

        private void BuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            if (scope != vsBuildScope.vsBuildScopeSolution) return;
            Fire(T.BuildStart);
        }

        private void BuildDone(vsBuildScope scope, vsBuildAction action)
        {
            if (scope != vsBuildScope.vsBuildScopeSolution) return;
            Fire(T.BuildStop);
        }

        private void ProjectBuildBegin(string projUniqueName, string projectConfig, string platform, string solutionConfig)
        {
            if (_slnMachine.IsInState(S.Building)) return;

            ThreadHelper.ThrowIfNotOnUIThread();
            if (!Dte.Solution.TryGetProjectByUniqueName(projUniqueName, out var project)) return;
            FireWs(WorkspaceTrigger.ProjectBuildStart, new RunnableName { RunnableId = project.Name });
        }

        private void ProjectBuildDone(string projUniqueName, string projectConfig, string platform, string solutionConfig, bool success)
        {
            if (_slnMachine.IsInState(S.Building)) return;

            ThreadHelper.ThrowIfNotOnUIThread();
            if (!Dte.Solution.TryGetProjectByUniqueName(projUniqueName, out var project)) return;
            FireWs(WorkspaceTrigger.ProjectBuildStop, new RunnableName { RunnableId = project.Name });
        }

        private void Fire(T trigger) => ThreadHelper.JoinableTaskFactory.Run(async () => await _slnMachine.FireAsync(trigger));
        private void Fire<TData>(T trigger, TData data) => ThreadHelper.JoinableTaskFactory.Run(async () => await _slnMachine.FireAsync(trigger.With<TData>(), data));
        private void FireWs<TData>(WorkspaceTrigger trigger, TData data) => ThreadHelper.JoinableTaskFactory.Run(async () => await _wsMachine.FireAsync(trigger.With<TData>(), data));
        private void FireWs(WorkspaceTrigger trigger) => ThreadHelper.JoinableTaskFactory.Run(async () => await _wsMachine.FireAsync(trigger));
    }
}