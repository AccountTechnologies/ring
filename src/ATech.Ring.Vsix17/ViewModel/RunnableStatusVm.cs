using System.Windows.Media;
using FontAwesome.WPF;

namespace ATech.Ring.Vsix.ViewModel
{
    public class RunnableStatusVm : FontAwesomeBaseIcon
    {
        public void Off()
        {
            Icon = FontAwesomeIcon.CircleOutline;
            IsSpinning = false;
            SpinDuration = 1;
            Foreground = Brushes.Gray;
            ActionIcon = FontAwesomeIcon.ArrowCircleUp;
        }

        public void Pending()
        {
            Icon = FontAwesomeIcon.CircleOutlineNotch;
            IsSpinning = true;
            SpinDuration = 1;
            Foreground = Brushes.Gray;
        }

        public void Debugging()
        {
            Icon = FontAwesomeIcon.Bug;
            IsSpinning = true;
            SpinDuration = 1;
            Foreground = Brushes.DarkTurquoise;
        }
        public void Building()
        {
            Icon = FontAwesomeIcon.Cog;
            IsSpinning = true;
            SpinDuration = 1;
            Foreground = Brushes.Gray;
        }

        public void HealthCheck()
        {
            Icon = FontAwesomeIcon.CircleOutlineNotch;
            IsSpinning = true;
            Foreground = Brushes.MediumSeaGreen;
            SpinDuration = 1;
            ActionIcon = FontAwesomeIcon.ArrowCircleDown;
        }
        public void Healthy()
        {
            Icon = FontAwesomeIcon.CircleOutline;
            IsSpinning = false;
            Foreground = Brushes.SpringGreen;
            ActionIcon = FontAwesomeIcon.ArrowCircleDown;
        }

        public void Dead()
        {
            Icon = FontAwesomeIcon.CircleOutline;
            IsSpinning = false;
            Foreground = Brushes.Red;
            ActionIcon = FontAwesomeIcon.Refresh;
        }

        public void Initiated()
        {
            Icon = FontAwesomeIcon.CircleOutlineNotch;
            IsSpinning = true;
            Foreground = Brushes.Yellow;
            SpinDuration = 1;
            ActionIcon = FontAwesomeIcon.ArrowCircleDown;
        }

        public void Started()
        {
            Icon = FontAwesomeIcon.CircleOutlineNotch;
            IsSpinning = true;
            Foreground = Brushes.LightSeaGreen;
            SpinDuration = 1;
            ActionIcon = FontAwesomeIcon.ArrowCircleDown;
        }

        public void Stopped()
        {
            Icon = FontAwesomeIcon.CircleOutline;
            IsSpinning = false;
            Foreground = Brushes.Gray;
            ActionIcon = FontAwesomeIcon.ArrowCircleUp;
        }

        public void Recovering()
        {
            Icon = FontAwesomeIcon.CircleOutlineNotch;
            IsSpinning = true;
            Foreground = Brushes.Orange;
            SpinDuration = 1;
            ActionIcon = FontAwesomeIcon.ArrowCircleDown;
        }
    }
}
