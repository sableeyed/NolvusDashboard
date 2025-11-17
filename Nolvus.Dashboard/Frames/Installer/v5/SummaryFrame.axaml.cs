using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Frames.Installer.v5
{
    public partial class SummaryFrame : DashboardFrame
    {
        public SummaryFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }
    }
}