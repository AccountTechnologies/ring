using System;
using System.Linq;
using System.Windows;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using ATech.Ring.Vsix.Client;
using ATech.Ring.Vsix.Client.Commands;

namespace ATech.Ring.Vsix.ViewModel
{
    public class RunnableVm : TreeVm
    {
        private enum T
        {
            Started, Stopped, Action, HealthCheckPending, HealthCheckHealthy,
            HealthCheckDead, Initiated, Recovering, Destroyed,
            BuildStart, DebugStart, BuildStop, DebugStop
        }

        private enum S
        {
            Zero, HealthCheck, Healthy, Dead, Initiated, Started, Stopped, Recovering, Active,
            Debugging, Building
        }

        private bool _isInSolution;
        private readonly Fsm _fsm = new Fsm();

        private RunnableStatusVm _status = new RunnableStatusVm();

        public RunnableVm(ISender<IRingCommand> sender, Action<string> writeLog, RunnableInfo model)
        {
            var logger = writeLog;

            _fsm.OnUnhandledTrigger((s, t) => writeLog($"Trigger '{t}' is not configured for state {s}"));
            _fsm.OnTransitioned(t => logger($"{UniqueId}: {t.Source} -> {t.Trigger} -> {t.Destination}"));

            _fsm.Configure(S.Zero)
                .OnActivate(() => _status.Off())
                .OnEntryFrom(T.Destroyed, () => _status.Off())
                .OnEntryFrom(T.DebugStop, Include)
                .OnEntryFrom(T.BuildStop, Include)
                .InternalTransition(T.Action, Include)
                .Permit(T.Initiated, S.Initiated)
                .Ignore(T.Stopped)
                .Ignore(T.Destroyed)
                .Ignore(T.DebugStart)
                .Ignore(T.DebugStop)
                .Ignore(T.BuildStart)
                .Ignore(T.BuildStop);

            _fsm.Configure(S.Initiated)
                .SubstateOf(S.Active)
                .OnEntryFrom(T.Initiated, () => _status.Initiated())
                .InternalTransition(T.Action, Exclude)
                .Permit(T.Started, S.Started);

            _fsm.Configure(S.Started)
                .SubstateOf(S.Active)
                .OnEntryFrom(T.Started, () => _status.Started())
                .InternalTransition(T.Action, Exclude)
                .Permit(T.HealthCheckPending, S.HealthCheck)
                .Permit(T.HealthCheckDead, S.Dead)
                .Permit(T.HealthCheckHealthy, S.Healthy)
                .Permit(T.Stopped, S.Stopped);

            _fsm.Configure(S.HealthCheck)
                .SubstateOf(S.Active)
                .OnEntryFrom(T.HealthCheckPending, () => _status.HealthCheck())
                .InternalTransition(T.Action, Exclude)
                .Ignore(T.HealthCheckPending)
                .Permit(T.Stopped, S.Stopped)
                .Permit(T.HealthCheckHealthy, S.Healthy)
                .Permit(T.HealthCheckDead, S.Dead)
                .Permit(T.Recovering, S.Recovering);

            _fsm.Configure(S.Healthy)
                .SubstateOf(S.Active)
                .OnEntryFrom(T.HealthCheckHealthy, () => _status.Healthy())
                .InternalTransition(T.Action, Exclude)
                .Permit(T.HealthCheckPending, S.HealthCheck)
                .PermitReentry(T.HealthCheckHealthy)
                .Permit(T.Stopped, S.Stopped);

            _fsm.Configure(S.Dead)
                .OnEntryFrom(T.HealthCheckDead, () => _status.Dead())
                .InternalTransition(T.Action, Restart)
                .Ignore(T.HealthCheckDead)
                .Permit(T.Stopped, S.Stopped);

            _fsm.Configure(S.Stopped)
                .OnEntryFrom(T.Stopped, () => _status.Stopped())
                .InternalTransition(T.Action, Include)
                .Permit(T.Destroyed, S.Zero)
                .Permit(T.Started, S.Started)
                .Permit(T.Initiated, S.Initiated);

            _fsm.Configure(S.Recovering)
                .SubstateOf(S.Active)
                .OnEntryFrom(T.Recovering, () => _status.Recovering())
                .InternalTransition(T.Action, Exclude)
                .Permit(T.Stopped, S.Stopped);

            _fsm.Configure(S.Debugging)
                .InternalTransition(T.Stopped, () => _status.Debugging())
                .Ignore(T.Destroyed)
                .OnEntryFrom(T.DebugStart, Exclude)
                .Permit(T.DebugStop, S.Zero);

            _fsm.Configure(S.Building)
                .InternalTransition(T.Stopped, () => _status.Building())
                .Ignore(T.Destroyed)
                .OnEntryFrom(T.BuildStart, Exclude)
                .Permit(T.BuildStop, S.Zero);

            _fsm.Configure(S.Active)
                .Permit(T.DebugStart, S.Debugging)
                .Permit(T.BuildStart, S.Building);

            _fsm.Activate();

            ModelRef = model;


            switch (model.State)
            {
                case RunnableState.ZERO:
                    break;
                case RunnableState.INITIATED:
                    Initiated();
                    break;
                case RunnableState.STARTED:
                    Initiated();
                    Started();
                    break;
                case RunnableState.HEALTH_CHECK:
                    Initiated();
                    Started();
                    HealthCheck();
                    break;
                case RunnableState.HEALTHY:
                    Initiated();
                    Started();
                    Healthy();
                    break;
                case RunnableState.DEAD:
                    Initiated();
                    Started();
                    Dead();
                    break;
                case RunnableState.RECOVERING:
                    Initiated();
                    Started();
                    Recovering();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            void Restart()
            {
                Exclude();
                Include();
            }

            void Exclude()
            {
                sender.Exclude(this);
                _status.Pending();
            }

            void Include()
            {
                sender.Include(this);
                _status.Pending();
            }
        }

        public RunnableStatusVm Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public string UniqueId => Model.Id;

        public RunnableInfo Model => (RunnableInfo)ModelRef;
        public Visibility ShowRevealInOctant => Model.Details.ContainsKey(DetailsKeys.KubernetesPods) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowBrowseUri => Model.Details.ContainsKey(DetailsKeys.Uri) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ShowOpenFolder => Model.Details.ContainsKey(DetailsKeys.WorkDir) ? Visibility.Visible : Visibility.Collapsed;

        public override string DisplayName => Model.Details.ContainsKey(DetailsKeys.FriendlyName) ? (string)Model.Details[DetailsKeys.FriendlyName] : Model.Id;

        public bool IsInSolution
        {
            get => _isInSolution;
            set
            {
                if (!Model.TryGetCsProjPath(out _)) return;
                _isInSolution = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayNameFontWeight));
            }
        }

        public FontWeight DisplayNameFontWeight => IsInSolution ? FontWeights.Bold : FontWeights.Normal;

        public void Destroyed() => _fsm.Fire(T.Destroyed);
        public void Recovering() => _fsm.Fire(T.Recovering);
        public void Stopped() => _fsm.Fire(T.Stopped);
        public void Started() => _fsm.Fire(T.Started);
        public void Initiated() => _fsm.Fire(T.Initiated);
        public void Dead() => _fsm.Fire(T.HealthCheckDead);
        public void Healthy() => _fsm.Fire(T.HealthCheckHealthy);
        public void HealthCheck() => _fsm.Fire(T.HealthCheckPending);
        public void Action() => _fsm.Fire(T.Action);
        public void DebugStart() => _fsm.Fire(T.DebugStart);
        public void DebugStop() => _fsm.Fire(T.DebugStop);
        public void BuildStart() => _fsm.Fire(T.BuildStart);
        public void BuildStop() => _fsm.Fire(T.BuildStop);

        private class Fsm : Stateless.StateMachine<S, T>
        {
            public Fsm() : base(S.Zero) { }
        }
    }
}
