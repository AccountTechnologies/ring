using System.ComponentModel;
using System.Runtime.CompilerServices;
using ATech.Ring.Vsix.Annotations;

namespace ATech.Ring.Vsix.ViewModel
{
    public abstract class TreeVm : INotifyPropertyChanged
    {
        public abstract string DisplayName { get; }

        public object ModelRef
        {
            get => _modelRef;
            protected set
            {
                _modelRef = value;
                OnPropertyChanged();
            }
        }

        private SubWorkspaceVm _parent;
        private object _modelRef;

        public SubWorkspaceVm Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}