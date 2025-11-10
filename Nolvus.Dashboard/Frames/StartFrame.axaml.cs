using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Api.Installer.Library;
using Nolvus.Api.Installer.Services;
using Nolvus.Package.Conditions;
using Nolvus.Dashboard.Controls;
using Nolvus.Core.Enums;
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
                    Console.WriteLine("No Installation found - running install sequence");
                    ServiceSingleton.Dashboard.LoadFrame<GameFrame>();
                }
                else
                {   
                    var owner = TopLevel.GetTopLevel(this) as Window;
                    //Console.WriteLine("Unimpl StartFrame Exception");
                    //await NolvusMessageBox.Show(owner, "Error", "You have a .ini present but installation is not implemented", MessageBoxType.Error);
                    await CheckNolvus();
                    // await CheckForUpdates();
                    // await CheckNexus();

                    // var InstancesCheck = await CheckInstances();

                    // ServiceSingleton.Dashboard.EnableSettings();
                }
            } catch (Exception ex)
            {
                ServiceSingleton.Logger.Log(ex.StackTrace);
            }

        }

        private async Task CheckNolvus()
        {
            try 
            {
                ServiceSingleton.Dashboard.Progress(0);
                ServiceSingleton.Dashboard.Status("Connecting to Nolvus...");
                ServiceSingleton.Logger.Log("Connecting to Nolvus...");

                var ApiUrl = ServiceSingleton.Globals.ApiUrl;
                var ApiVersion = ServiceSingleton.Globals.ApiVersion;
                var UserName = ServiceSingleton.Globals.NolvusUserName;
                var Password = ServiceSingleton.Globals.NolvusPassword;

                if (ApiVersion == string.Empty || UserName == string.Empty || Password == string.Empty)
                {
                    throw new Exception("Nolvus settings missing, please check your Nolvus settings!");
                }
                else
                {
                    ApiManager.Init(ApiUrl, ApiVersion, UserName, ServiceSingleton.Lib.DecryptString(Password));

                    if (!await ApiManager.Service.Installer.Authenticate(UserName, ServiceSingleton.Lib.DecryptString(Password)))
                    {
                        throw new Exception("Invalid Nolvus user name / password or your account has not been activated!");
                    }

                    ServiceSingleton.Dashboard.Progress(25);

                    ServiceSingleton.Logger.Log("Connected to Nolvus");
                }
            }
            catch (Exception ex) 
            {
                Exception CaughtExeption = ex;

                if (ex.InnerException != null)
                {
                    CaughtExeption = ex.InnerException;
                }

                throw new Exception("Error during Nolvus connection. The Nolvus web site may have issues currently. Original message : " + CaughtExeption + ")");
            }

            // ServiceSingleton.Dashboard.Status("Connecting to Nolvus...");

            // var url = ServiceSingleton.Globals.ApiUrl;
            // var version = ServiceSingleton.Globals.ApiVersion;
            // var user = ServiceSingleton.Globals.NolvusUserName;
            // var pass = ServiceSingleton.Globals.NolvusPassword;

            // if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            //     throw new Exception("Nolvus settings missing, please check your Nolvus settings!");

            // ApiManager.Init(url, version, user, ServiceSingleton.Lib.DecryptString(pass));

            // if (!await ApiManager.Service.Installer.Authenticate(user, ServiceSingleton.Lib.DecryptString(pass)))
            //     throw new Exception("Invalid Nolvus credentials or account not activated!");

            // ServiceSingleton.Dashboard.Progress(25);
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
