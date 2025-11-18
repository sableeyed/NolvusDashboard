using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class InstallFrame : DashboardFrame
    {
        public InstallFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        private async void OnClickTest(object? sender, RoutedEventArgs e)
        {
            
        }
    }
}