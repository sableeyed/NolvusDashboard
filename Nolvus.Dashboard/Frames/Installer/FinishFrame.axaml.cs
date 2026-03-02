using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Misc;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames.Instance;
using System.Diagnostics;
using Avalonia.Controls;

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

        public string InstanceName
        {
            get
            {
                if (!Parameters.IsEmpty && Parameters["Instance"] != null)
                {
                    return Parameters["Instance"].ToString();
                }

                return string.Empty;
            }
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Info("Installation completed");
            ServiceSingleton.Dashboard.ProgressCompleted();
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

        private void BtnGuide_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            switch (InstanceName)
            {
                case Strings.NolvusAscension:
                    Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/asc/appendix/player-guide") { UseShellExecute = true });
                    break;

                case Strings.NolvusAwakening:
                    Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide") { UseShellExecute = true });
                    break;
                default:
                    NolvusMessageBox.Show(owner, "User Guide", "An error occured while trying to access the user guide!", MessageBoxType.Error);
                    break;
            }  
        }
    }
}