using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Dashboard.Frames.Instance.v6
{
    public partial class KeysBindingFrame : DashboardFrame
    {
        public KeysBindingFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnBack.Click += BtnBack_Click;
            BtnController.Click += BtnController_Click;
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