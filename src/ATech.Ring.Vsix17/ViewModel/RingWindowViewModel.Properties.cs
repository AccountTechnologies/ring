using System.ComponentModel;
using System.Runtime.CompilerServices;
using ATech.Ring.Vsix.Annotations;

namespace ATech.Ring.Vsix.ViewModel
{
    public partial class RingWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
