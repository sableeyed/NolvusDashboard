using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames.Instance.v6
{
    public partial class KeysBindingFrame : DashboardFrame
    {
        public KeysBindingFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnFullGuide.Click += BtnFullGuide_Click;
            BtnBack.Click += BtnBack_Click;
            BtnController.Click += BtnController_Click;
        }

        private void BtnFullGuide_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/keybinds/keyboard") { UseShellExecute = true });
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private void BtnController_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<ControllerBindingFrame>();
        }
    }
}