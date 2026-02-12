using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Frames.Instance;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class FinishFrame : DashboardFrame
    {
        public FinishFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnContinue.Click += BtnContinue_Click;
            BtnPatreon.Click += BtnPatreon_Click;
            BtnDonate.Click += BtnDonate_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Info("Installation completed");
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private void BtnPatreon_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var url = "https://www.patreon.com/nolvus";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void BtnDonate_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var url = "https://ko-fi.com/mel49542";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

    }
}