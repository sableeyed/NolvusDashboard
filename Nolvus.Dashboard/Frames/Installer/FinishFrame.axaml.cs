using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

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
            Console.WriteLine("Manage Instances: Unimplemented");
        }

        private void BtnPatreon_Click(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("Patreon: Unimplemented");
        }

        private void BtnDonate_Click(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("Donate: Unimplemented");
        }

    }
}