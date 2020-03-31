using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ATech.Ring.Vsix
{
    [Guid("fb92b746-a77d-4285-bfa1-3e7a8c8b3d9e")]
    public class RingWindow : ToolWindowPane
    {
        public RingWindow(RingWindowControl control)
        {
            Caption = "ring!";
            Content = control;
        }
    }
}
