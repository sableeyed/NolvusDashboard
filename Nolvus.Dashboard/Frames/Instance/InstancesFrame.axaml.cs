using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Frames.Installer;

namespace Nolvus.Dashboard.Frames.Instance
{
    public partial class InstancesFrame : DashboardFrame
    {
        public InstancesFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }
    }
}