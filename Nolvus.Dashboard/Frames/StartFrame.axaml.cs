using System;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Api.Installer.Library;
using Nolvus.Api.Installer.Services;
using Nolvus.Package.Conditions;
// using Nolvus.Dashboard.Frames.Installer;
// using Nolvus.Dashboard.Frames.Instance;
// using Nolvus.Dashboard.Frames.Settings;

namespace Nolvus.Dashboard.Frames
{
    public partial class StartFrame : DashboardFrame
    {
        public StartFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();
        }

        public StartFrame() : this(ServiceSingleton.Dashboard, new FrameParameters()) { }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NolvusDashboard.ini")))
                {
                    ServiceSingleton.Dashboard.LoadFrame<GameFrame>();
                }
                else
                {
                    Console.WriteLine("Unimpl StartFrame Exception");
                }
            } catch (Exception ex)
            {
                ServiceSingleton.Logger.Log(ex.StackTrace);
            }

        }

        private async Task CheckNolvus()
        {
            ServiceSingleton.Dashboard.Status("Connecting to Nolvus...");

            var url = ServiceSingleton.Globals.ApiUrl;
            var version = ServiceSingleton.Globals.ApiVersion;
            var user = ServiceSingleton.Globals.NolvusUserName;
            var pass = ServiceSingleton.Globals.NolvusPassword;

            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                throw new Exception("Nolvus settings missing, please check your Nolvus settings!");

            ApiManager.Init(url, version, user, ServiceSingleton.Lib.DecryptString(pass));

            if (!await ApiManager.Service.Installer.Authenticate(user, ServiceSingleton.Lib.DecryptString(pass)))
                throw new Exception("Invalid Nolvus credentials or account not activated!");

            ServiceSingleton.Dashboard.Progress(25);
        }

        private async Task CheckForUpdates()
        {
            ServiceSingleton.Dashboard.Status("Checking for updates...");

            var latest = await ApiManager.Service.Installer.GetLatestInstaller();
            ServiceSingleton.Dashboard.Progress(50);

            if (ServiceSingleton.Dashboard.IsOlder(latest.Version))
            {
                // (We keep the updater logic exactly the same)
                if (!ServiceSingleton.Updater.Installed ||
                    !await ServiceSingleton.Updater.IsValid(latest.UpdaterHash) ||
                    ServiceSingleton.Updater.IsOlder(latest.UpdaterVersion))
                {
                    await ServiceSingleton.Files.DownloadFile(latest.UpdaterLink,
                        ServiceSingleton.Updater.UpdaterExe,
                        (s, e) =>
                        {
                            ServiceSingleton.Dashboard.Status($"Downloading Updater ({e.ProgressPercentage}%)");
                            ServiceSingleton.Dashboard.Progress(e.ProgressPercentage);
                        });
                }

                await ServiceSingleton.Updater.Launch();
                ServiceSingleton.Dashboard.ShutDown();
            }
        }

        private async Task CheckNexus()
        {
            ServiceSingleton.Dashboard.Status("Connecting to Nexus...");

            ServiceSingleton.Files.RemoveDirectory(ServiceSingleton.Folders.NexusCacheDirectory, false);

            var key = ServiceSingleton.Globals.NexusApiKey;
            var agent = ServiceSingleton.Globals.NexusUserAgent;

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(agent))
                throw new Exception("Nexus API key or User Agent missing!");

            NexusApi.ApiManager.Init(key, agent, ServiceSingleton.Folders.NexusCacheDirectory);

            ServiceSingleton.Dashboard.TitleInfo(NexusApi.ApiManager.AccountInfo.Name);
            ServiceSingleton.Dashboard.NexusAccount(ApiManager.Service.Installer.LoggedUser + "@" + NexusApi.ApiManager.AccountInfo.Name);

            if (NexusApi.ApiManager.AccountInfo.IsPremium)
                ServiceSingleton.Dashboard.AccountType("(Premium)");
            else if (NexusApi.ApiManager.AccountInfo.IsSupporter)
                ServiceSingleton.Dashboard.AccountType("(Supporter)");
            else
                ServiceSingleton.Dashboard.AccountType("(Default)");

            ServiceSingleton.Dashboard.Progress(75);
        }

        private async Task<InstanceCheck> CheckInstances()
        {
            ServiceSingleton.Dashboard.Status("Checking instances...");

            ServiceSingleton.Instances.Load();

            if (!ServiceSingleton.Instances.Empty)
            {
                if (!ServiceSingleton.Instances.CheckInstances(out var msg))
                    throw new Exception(msg + " (Check InstancesData.xml)");

                return ServiceSingleton.Instances.InstancesToResume.Count > 0
                    ? InstanceCheck.InstancesToResume
                    : InstanceCheck.InstalledInstances;
            }

            return InstanceCheck.NoInstance;
        }
    }
}
