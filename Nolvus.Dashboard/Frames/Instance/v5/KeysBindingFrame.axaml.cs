using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Avalonia.Interactivity;

namespace Nolvus.Dashboard.Frames.Instance.v5
{
    public partial class KeysBindingFrame : DashboardFrame
    {
        public KeysBindingFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnBack.Click += BtnBack_Click;
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }
    }
}