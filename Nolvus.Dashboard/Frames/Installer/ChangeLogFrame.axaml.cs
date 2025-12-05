using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;

//BIG TODO
namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class ChangeLogFrame : DashboardFrame
    {
        public ChangeLogFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }
    }
}