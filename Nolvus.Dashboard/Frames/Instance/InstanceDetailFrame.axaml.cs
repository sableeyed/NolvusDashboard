using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Frames.Instance
{
    public partial class InstanceDetailFrame : DashboardFrame
    {
        public InstanceDetailFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }
    }
}