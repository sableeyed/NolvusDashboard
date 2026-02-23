using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Frames.Remap.v6
{
    public partial class RemapInstanceFrame : DashboardFrame
    {
        public RemapInstanceFrame(IDashboard Dashboard, FrameParameters Params):base(Dashboard, Params)
        {
            InitializeComponent();
        }
    }
}