using FontAwesome.WPF;
using System.Windows.Media;

namespace ATech.Ring.Vsix.ViewModel
{
    public class WorkspaceStatusVm : FontAwesomeBaseIcon
    {
        public WorkspaceStatusVm()
        {
            None();
        }
        public void Degraded()
        {
            Icon = FontAwesomeIcon.CircleOutlineNotch;
            Foreground = Brushes.Orange;
            IsSpinning = true;
        }

        public void None()
        {
            Icon = FontAwesomeIcon.None;
            Foreground = Brushes.Transparent;
            IsSpinning = false;
        }

        public void Idle()
        {
            Icon = FontAwesomeIcon.CircleOutline;
            Foreground = Brushes.Gray;
            IsSpinning = false;
        }

        public void Pending()
        {
            Icon = FontAwesomeIcon.CircleOutlineNotch;
            Foreground = Brushes.Gray;
            IsSpinning = true;
        }

        public void Healthy()
        {
            Icon = FontAwesomeIcon.CircleOutline;
            Foreground = Brushes.MediumSpringGreen;
            IsSpinning = false;
        }
    }
}