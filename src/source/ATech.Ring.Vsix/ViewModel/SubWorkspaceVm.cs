using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ATech.Ring.Protocol;
using ATech.Ring.Protocol.Events;
using ATech.Ring.Vsix.Client.Commands;

namespace ATech.Ring.Vsix.ViewModel
{
    public class SubWorkspaceVm : TreeVm
    {
        private readonly ISender<IRingCommand> _sender;
        private readonly Action<string> _writeLog;
        public SubWorkspaceVm(ISender<IRingCommand> sender, Action<string> writeLog, WorkspaceInfo modelRef)
        {
            _sender = sender;
            _writeLog = writeLog;
            ModelRef = modelRef;
        }

        public SubWorkspaceVm MapWorkspace(WorkspaceInfo config)
        {
            ModelRef = config;
            return MapWorkspace(this, config);

            SubWorkspaceVm MapWorkspace(SubWorkspaceVm parent, WorkspaceInfo cfg)
            {
                var w = new SubWorkspaceVm(_sender, _writeLog, cfg) { Parent = parent };

                MapRunnables(w, cfg);
                return w;

                void MapRunnables(SubWorkspaceVm p, WorkspaceInfo c)
                {
                    var runnables = c.Runnables;
                    if (!runnables.Any()) return;

                    foreach (var r in runnables.Select(r => MapRunnable(p, r))) p.Runnables.Add(r);
                }

                RunnableVm MapRunnable(SubWorkspaceVm p, RunnableInfo c)
                => new RunnableVm(_sender, _writeLog, c)
                {
                    Parent = p
                };
            }
        }

        public override string DisplayName => ((WorkspaceInfo)ModelRef).Path;

        private ObservableCollection<SubWorkspaceVm> _imports = new ObservableCollection<SubWorkspaceVm>();

        public ObservableCollection<SubWorkspaceVm> Imports
        {
            get => _imports;
            set
            {
                _imports = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasImports));
            }
        }

        private ObservableCollection<RunnableVm> _runnables = new ObservableCollection<RunnableVm>();

        public ObservableCollection<RunnableVm> Runnables
        {
            get => _runnables;
            set
            {
                _runnables = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasRunnables));
            }
        }

        public Visibility HasImports => _imports.Any() ? Visibility.Visible : Visibility.Collapsed;
        public Visibility HasRunnables => _runnables.Any() ? Visibility.Visible : Visibility.Collapsed;
    }
}