using ATech.Ring.Vsix.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ATech.Ring.Vsix.Client;

namespace ATech.Ring.Vsix.ViewModel
{
    public class RunnablesViewModel : INotifyPropertyChanged
    {
        private readonly Action<string> _writeLog;

        public RunnablesViewModel(Action<string> writeLog)
        {
            _writeLog = writeLog;
        }

        public RunnablesViewModel Map(SubWorkspaceVm workspace)
        {
            foreach (var r in Map(workspace)) Items.Add(r);
            return this;

            IEnumerable<RunnableVm> Map(SubWorkspaceVm w)
            {
                IEnumerable<RunnableVm> DeDupe(IEnumerable<RunnableVm> rs) => rs.GroupBy(r => r.DisplayName).Select(x => x.First());
                return DeDupe(w.Runnables).Concat(w.Imports.Any() ? DeDupe(w.Imports.SelectMany(Map)) : Enumerable.Empty<RunnableVm>());
            }
        }

        private ObservableCollection<RunnableVm> _runnables = new ObservableCollection<RunnableVm>();

        private bool _noRunnablesInSolution = false;
        public ObservableCollection<RunnableVm> Items
        {
            get => _runnables;
            set
            {
                _runnables = value;
                OnPropertyChanged();
            }
        }

        public void SetAllBuilding()
        {
            Items.ForEach(action: r => r.BuildStart(), where: r => r.IsInSolution);
        }

        public void UnsetAllBuilding()
        {
            Items.ForEach(action: r => r.BuildStop(), where: r => r.IsInSolution);
        }

        public void UnsetInSolution()
        {
            foreach (var r in Items) r.IsInSolution = false;
            NoRunnablesInSolution = false;
        }

        public void SetInSolution(string[] projectNames)
        {
            _writeLog("Setting IsInSolution flag.");
            foreach (var pn in projectNames) _writeLog($"S: {pn}");


            NoRunnablesInSolution = true;
            foreach (var r in Items)
            {
                if (!r.Model.TryGetCsProjPath(out var fullPath)) continue;

                _writeLog($"W: {fullPath}");
                if (!projectNames.Any(x => x.Equals(fullPath, StringComparison.OrdinalIgnoreCase))) continue;
                r.IsInSolution = true;
                NoRunnablesInSolution = false;
            }
        }

        public void StopBeforeDebug(string[] uniqueIds) => Items.ForEach(r => r.DebugStart(), uniqueIds);

        public bool NoRunnablesInSolution
        {
            get => _noRunnablesInSolution;
            set
            {
                _noRunnablesInSolution = value;
                OnPropertyChanged();
            }
        }

        public void Reset()
        {
            Items.Clear();
            NoRunnablesInSolution = false;
        }

        public IEnumerable<RunnableVm> this[Func<RunnableVm,bool> where] => _runnables.Where(where);

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal static class RunnablesExtensions
    {
        internal static void ForEach(this IEnumerable<RunnableVm> runnables, Action<RunnableVm> action, Func<RunnableVm,bool> where)
        {
            foreach (var r in runnables.Where(where)) action(r);
        }

        internal static void ForEach(this IEnumerable<RunnableVm> runnables, Action<RunnableVm> action, params string[] ids)
        {
            foreach (var r in runnables.FindByIds(ids)) action(r);
        }
        internal static IEnumerable<RunnableVm> FindByIds(this IEnumerable<RunnableVm> runnables, IEnumerable<string> ids) => runnables.Where(r => ids.Any(id => id.Equals(r.Model.Id, StringComparison.OrdinalIgnoreCase)));
    }
}