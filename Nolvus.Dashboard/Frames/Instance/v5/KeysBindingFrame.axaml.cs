using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Frames.Instance.v5
{
    public partial class KeysBindingFrame : DashboardFrame
    {
        public KeysBindingFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }
    }
}