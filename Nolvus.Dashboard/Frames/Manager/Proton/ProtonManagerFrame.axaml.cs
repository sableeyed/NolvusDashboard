using System;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Frames.Manager.Proton
{
    public partial class ProtonManagerFrame : DashboardFrame
    {
        public ProtonManagerFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        protected override async Task OnLoadedAsync()
        {
            
        }
    }
}