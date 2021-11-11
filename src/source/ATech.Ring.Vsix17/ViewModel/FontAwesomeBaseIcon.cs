using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ATech.Ring.Vsix.Annotations;
using FontAwesome.WPF;

namespace ATech.Ring.Vsix.ViewModel
{
    public abstract class FontAwesomeBaseIcon : INotifyPropertyChanged
    {
        private FontAwesomeIcon _actionIcon;
        public FontAwesomeIcon ActionIcon
        {
            get => _actionIcon;
            set
            {
                _actionIcon = value;
                OnPropertyChanged();
            }
        }

        private FontAwesomeIcon _icon;
        public FontAwesomeIcon Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        private int _spinDuration = 1;
        public int SpinDuration
        {
            get => _spinDuration;
            set
            {
                _spinDuration = value;
                OnPropertyChanged();
            }
        }
        private bool _isSpinning = false;
        public bool IsSpinning
        {
            get => _isSpinning;
            set
            {
                _isSpinning = value;
                OnPropertyChanged();
            }
        }

        private Brush _foreground;
        public Brush Foreground
        {
            get => _foreground;
            set
            {
                _foreground = value;
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
