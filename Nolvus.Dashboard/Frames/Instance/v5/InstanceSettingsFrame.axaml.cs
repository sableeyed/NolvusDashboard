using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Frames.Instance.v5
{
    public partial class InstanceSettingsFrame : DashboardFrame
    {
        public InstanceSettingsFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }
    }
}