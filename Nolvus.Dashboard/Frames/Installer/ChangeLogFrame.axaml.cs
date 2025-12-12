using Avalonia.Controls;
using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Controls;
using Nolvus.Core.Enums;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Dashboard.Frames.Instance;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class ChangeLogFrame : DashboardFrame
    {
        private string _FromVersion;
        private string _ToVersion;
        public ChangeLogFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnContinue.Click += BtnContinue_Click;
            BtnPrevious.Click += BtnPrevious_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Info("Change Log");

            INolvusInstance Instance = Parameters["Instance"] as INolvusInstance;

            _FromVersion = Instance.Version;
            _ToVersion = await Instance.GetLatestVersion();

            ServiceSingleton.Dashboard.Status("Loading...");

            //Too lazy to fix CEF at the moment
            var owner = TopLevel.GetTopLevel(this) as Window;
            bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Change Logs", "Would you like to view the change logs for this update? This will open an external browser instance.");
            if (result == true)
            {
                var url = "https://www.nolvus.net/appendix/changelog?from=" + _FromVersion + "&to=" + _ToVersion;
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (await (Parameters["Instance"] as INolvusInstance).LatestPackageRequireReInstall())
            {
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Warning", "This new version requires a new game and new installation. Do you want to proceed?");
                if (result == true)
                {
                    ServiceSingleton.Dashboard.LoadFrame<DeleteFrame>(new FrameParameters(new FrameParameter() { Key = "Instance", Value = Parameters["Instance"] as INolvusInstance }, new FrameParameter() { Key = "Action", Value = InstanceAction.Delete }));
                }
            }
            else if (await (Parameters["Instance"] as INolvusInstance).LatestPackageRequireNewGame())
            {
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Warning", "This new version requires a new game. Your current saves will not work. Do you want to proceed?");
                if (result == true)
                {
                    StartUpdate();
                }
            }
            else
            {
                StartUpdate();
            }
        }

        private async void StartUpdate()
        {
            ServiceSingleton.Instances.WorkingInstance = (Parameters["Instance"] as INolvusInstance);
            ServiceSingleton.Instances.PrepareInstanceForUpdate();
            await ServiceSingleton.Dashboard.LoadFrameAsync<PackageFrame>();
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }
    }
}