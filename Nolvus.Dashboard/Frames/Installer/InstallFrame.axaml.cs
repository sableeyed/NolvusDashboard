using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Core.Events;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Nolvus.Dashboard.Controls;
using Nolvus.Browser;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class InstallFrame : DashboardFrame
    {
        public InstallFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                // var Instance = ServiceSingleton.Instances.WorkingInstance;
                
                // ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Instance Auto Installer]");
                // ServiceSingleton.Dashboard.Status(string.Format("Installing {0} - {1} (v{2})", Instance.Name, Instance.Performance.Variant, ServiceSingleton.Packages.LoadedVersion));
                // ServiceSingleton.Dashboard.AdditionalSecondaryInfo("Error(s) : 0");

                // GlobalProgress();

                // ServiceSingleton.Files.RemoveDirectory(ServiceSingleton.Folders.NexusCacheDirectory, false);                                             

                // await ServiceSingleton.Packages.InstallModList(new ModInstallSettings()
                // {
                //     OnStartInstalling = () =>
                //     {
                //         Refresh(ServiceSingleton.Settings.RefreshInterval);
                //     },
                //     OnModInstalled = (Mod) =>
                //     {
                //         GlobalProgress();
                //         ServiceSingleton.Logger.Log(string.Format("Mod : {0} installed.", Mod.Name));
                //     }, 
                //     OnModError = (ErrorCount) => 
                //     {
                //         if (ServiceSingleton.Packages.ErrorHandler.ThresholdEnabled)
                //         {
                //             ServiceSingleton.Dashboard.AdditionalSecondaryInfo(string.Format("Error(s) : {0} Threshold : {1} {2}", ServiceSingleton.Packages.ErrorHandler.ErrorsCount, ServiceSingleton.Settings.ErrorsThreshold, "(Errors will be displayed at the end of the installation)"));
                //         }
                //         else
                //         {
                //             ServiceSingleton.Dashboard.AdditionalSecondaryInfo(string.Format("Error(s) : {0} {1}", ServiceSingleton.Packages.ErrorHandler.ErrorsCount, "(Errors will be displayed at the end of the installation)"));
                //         }
                //     },
                //     OnMaxErrors = () =>
                //     {
                //         ServiceSingleton.Dashboard.AdditionalSecondaryInfo(string.Format("Error(s) : {0} {1}", ServiceSingleton.Packages.ErrorHandler.ErrorsCount, "(Maximum errors threshold reached, waiting for current queue to finish...)"));
                //     },               
                //     Browser = () =>
                //     {
                //         return Invoke((Func<IBrowserInstance>)(() => { return new BrowserWindow(); })) as IBrowserInstance;
                //     }
                // });                

                Console.WriteLine("InstallFrame: Unimplemented Load Order Frame");
                //await ServiceSingleton.Dashboard.LoadFrameAsync<LoadOrderFrame>(new FrameParameters(new FrameParameter(){Key = "Mode", Value = "Install"}));
            }
            catch
            {
                ServiceSingleton.Dashboard.ClearInfo();  
                Console.WriteLine("InstallFrame: Unimplemented Error Summary Frame");           
                //ServiceSingleton.Dashboard.LoadFrame<ErrorSummaryFrame>();                
            }
        }

        private void GlobalProgress()
        {
            ServiceSingleton.Dashboard.Info(string.Format("Installing mods ({0}%)",ServiceSingleton.Packages.InstallProgression));
            ServiceSingleton.Dashboard.AdditionalInfo(string.Format("Mods {0}/{1}", ServiceSingleton.Instances.WorkingInstance.Status.InstalledMods.Count, ServiceSingleton.Packages.ModsCount));   
        }

        private void RefreshBox()
        {
            //TODO?
        }

        private void Refresh(int Ms)
        {
            //TODO?
        }

    }
}