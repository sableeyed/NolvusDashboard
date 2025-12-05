using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Dashboard.Frames.Instance.v6
{
    public partial class ControllerBindingFrame : DashboardFrame
    {
        public ControllerBindingFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnBack.Click += BtnBack_Click;
            BtnKeyboard.Click += BtnKeyboard_Click;
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