using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames.Instance.v6
{
    public partial class ControllerBindingFrame : DashboardFrame
    {
        public ControllerBindingFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnFullGuide.Click += BtnFullGuide_Click;
            BtnBack.Click += BtnBack_Click;
            BtnKeyboard.Click += BtnKeyboard_Click;
        }

        private void BtnFullGuide_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/keybinds/controller") { UseShellExecute = true });
        }
        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private void BtnKeyboard_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<v6.KeysBindingFrame>();
        }
    }
}