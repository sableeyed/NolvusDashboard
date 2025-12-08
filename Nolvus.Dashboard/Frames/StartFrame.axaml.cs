using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Vcc.Nolvus.Api.Installer.Library;
using Vcc.Nolvus.Api.Installer.Services;
using Nolvus.Package.Conditions;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames.Installer;
using Nolvus.Dashboard.Frames.Instance;
using Nolvus.Dashboard.Frames.Settings;

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
                //no ini file, proceed to Game setup + Nexus SSO + Nolvus auth
                if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NolvusDashboard.ini")))
                {
                    ServiceSingleton.Dashboard.LoadFrame<GameFrame>();
                }
                else //valid ini file - proceed to install
                {   
                    //make titlebar picture visible
                    var window = TopLevel.GetTopLevel(this) as DashboardWindow;
                    window?.SetAccountImageVisible(true);
                    await CheckNolvus();
                    await CheckForUpdates();
                    await CheckNexus();

                    var InstancesCheck = await CheckInstances();
                    ServiceSingleton.Dashboard.EnableSettings();
                    switch (InstancesCheck)
                    {
                        case InstanceCheck.NoInstance:
                            ServiceSingleton.Logger.Log("Dashboard is ready to install");
                            await ServiceSingleton.Dashboard.LoadFrameAsync<SelectInstanceFrame>();
                            break;
                        case InstanceCheck.InstancesToResume:
                            ServiceSingleton.Logger.Log("Dashboard is ready to resume");
                            ServiceSingleton.Dashboard.LoadFrame<ResumeFrame>();
                            break;
                        case InstanceCheck.InstalledInstances:
                            ServiceSingleton.Logger.Log("Dashboard ready to play");
                            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
                            break;
                    }
                }
            } catch (Exception ex)
            {
                ServiceSingleton.Logger.Log(ex.StackTrace);
            }

            ServiceSingleton.Dashboard.NoStatus();
            ServiceSingleton.Dashboard.ProgressCompleted();

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

                ServiceSingleton.Logger.Log("Error reading NolvusDashboard.ini - If you are using the terminal to run this program, make sure you are in the same directory instead of using the full path.");
                throw new Exception("Error during Nolvus connection. The Nolvus web site may have issues currently. Original message : " + CaughtExeption + ")");
            }
        }

        private async Task CheckForUpdates()
        {
            ServiceSingleton.Dashboard.Status("Checking for updates...");
            ServiceSingleton.Logger.Log("Checking for updates...");
            ServiceSingleton.Logger.Log("StartFrame.CheckForUpdates: STUB");

            var latest = await ApiManager.Service.Installer.GetLatestInstaller();
            ServiceSingleton.Dashboard.Progress(50);

            if (ServiceSingleton.Dashboard.IsOlder(latest.Version))
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Update Required", "You must update the dashboard manually, would you like to open the releases page?");
                if (result == true)
                {
                    var url = "https://www.nolvus.net/appendix/installer/faq";
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
                Environment.Exit(0);
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
            try 
            {
                ServiceSingleton.Dashboard.Status("Checking instances...");
                ServiceSingleton.Logger.Log("Checking instances...");

                ServiceSingleton.Instances.Load();

                if (!ServiceSingleton.Instances.Empty)
                {
                    var InstanceMessage = string.Empty;

                    if (ServiceSingleton.Instances.CheckInstances(out InstanceMessage))
                    {
                        if (ServiceSingleton.Instances.InstancesToResume.Count > 0)
                        {
                            return InstanceCheck.InstancesToResume;
                        }
                        else
                        {
                            return InstanceCheck.InstalledInstances;
                        }
                    }
                    else
                    {
                        throw new Exception(InstanceMessage + ".This can happen if you modified the file InstancesData.xml manually!");
                    }
                }
                return InstanceCheck.NoInstance;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception("Error during instance checking with error : " + ex.Message + ". Certainly due to a manual editing of the InstancesData.xml file!");
            }
        }
    }
}
