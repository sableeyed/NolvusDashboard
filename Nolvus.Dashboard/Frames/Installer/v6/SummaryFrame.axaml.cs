using Avalonia.Interactivity;
using Avalonia.Media;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Nolvus.NexusApi;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class SummaryFrame : DashboardFrame
    {
        public SummaryFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnPrevious.Click += BtnPrevious_Click;
            BtnStart.Click += BtnStart_Click;
        }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

                LblName.Text = Instance.Name;
                LblResolution.Text = Instance.Settings.Width + "x" + Instance.Settings.Height;
                LblRatio.Text = Instance.Settings.Ratio;
                LblInstallDir.Text = Instance.InstallDir;
                LblArchiveDir.Text = Instance.ArchiveDir;

                LblEnableArchiving.Text = Instance.Settings.EnableArchiving ? "Yes" : "No";

                LblDownscaling.Text = Instance.Performance.DownScaling == "TRUE" ? "Yes (" + Instance.Performance.DownScaledResolution + ")" : "No";
                LblVariant.Text = Instance.Performance.Variant;
                LblAA.Text = Instance.Performance.AntiAliasing;

                switch (Instance.Performance.IniSettings)
                {
                    case "0": LblIni.Text = "Low";
                        break;
                    case "1":
                        LblIni.Text = "Medium";
                        break;
                    case "2":
                        LblIni.Text = "High";
                        break;
                }

                LblSREX.Text = Instance.Performance.SREX == "TRUE" ? "Yes" : "No";
                LblLODs.Text = Instance.Performance.LODs;
                LblFrameGen.Text = Instance.Performance.FrameGeneration == "TRUE" ? "Yes" : "No";
                

                LblNudity.Text = Instance.Options.Nudity == "TRUE" ? "Yes" : "No";
                LblCombatAnims.Text = Instance.Options.CombatAnimation;
                LblLeveling.Text = Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No";
                LblGore.Text = Instance.Options.Gore == "TRUE" ? "Yes" : "No";
                LblENB.Text = ENBs.GetENBByCode(Instance.Options.AlternateENB);

                LblCombatScaling.Text = Instance.Options.CombatScaling;
                LblExhaustion.Text = Instance.Options.Exhaustion == "TRUE" ? "Yes" : "No";
                LblNerfPA.Text = Instance.Options.NerfPA;
                LblBoss.Text = Instance.Options.Boss == "TRUE" ? "Yes" : "No";
                LblController.Text = Instance.Options.Controller == "TRUE" ? "Yes" : "No";
                LblUI.Text = Instance.Options.UI;

                if (!ApiManager.AccountInfo.IsPremium)
                {
                    NotificationBanner.Background = new SolidColorBrush(Colors.Red);
                    //LblNotification.Text = "You are not a Nexus Premium user. Download will not be automatic (you will have to click the download button for each mod) and bandwidth will be limited to 2 MB/s";
                    LblNotification.Text = "You are not a Nexus Premium user. Functionality for free users is not implemented at this time.";
                    PremiumImg.IsVisible = false;
                    NonPremiumImg.IsVisible = true;
                    BtnStart.IsEnabled = false;
                }
                //no need for an else because the default state of the UI components are for premium members

                if (ServiceSingleton.Settings.ErrorsThreshold == 1 || !NexusApi.ApiManager.AccountInfo.IsPremium)
                {
                    RdBtnOneError.IsChecked = true;
                    RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages (max errors can be set up in the Nolvus Dashboard.ini file)", 1);
                }
                else if (ServiceSingleton.Settings.ErrorsThreshold == 0)
                {
                    RdBtnNoThreshold.IsChecked = true;
                    RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages (max errors can be set up in the Nolvus Dashboard.ini file)", 50);
                }
                else
                {
                    RdBtnThreshold.IsChecked = true;
                    RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages (max errors can be set up in the Nolvus Dashboard.ini file)", ServiceSingleton.Settings.ErrorsThreshold);
                }

                ServiceSingleton.Dashboard.Info("Review your selections");
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during summary checking", ex.Message, ex.StackTrace);
            }
        }


        private void BtnStart_Click(object? sender, RoutedEventArgs e)
        {
            string Mo2Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModOrganizer");            

            if (Directory.Exists(Mo2Path))
            {                
                ServiceSingleton.Dashboard.Error("Global ModOrganizer instance detected", "The installer can not proceed to the installation because a global ModOrganizer instance has been detected. Read the message below to fix", "READ THIS TO FIX!!!" + Environment.NewLine + "All automated mod lists use portable instances, this way you can have multiple lists installed together." + Environment.NewLine + "If you want to install Nolvus, you need to remove this installed ModOrganizer global instance to avoid issues(make a backup before if it's sensitive)." + Environment.NewLine + "To know where your global instance is installed go to " + Mo2Path + "." + Environment.NewLine + "This folder may be hidden (be sure you disable hidden files and folder in Windows folder options if you don't see it)" + Environment.NewLine + "If you made a backup of your global instance and want to continue, just delete the " + Mo2Path + " folder" + Environment.NewLine + "DON'T REACTIVATE THIS GLOBAL INSTANCE AFTER INSTALLATION!!! YOUR NOLVUS MOD ORGANIZER WILL NOT WORK!!!");
            }
            else
            {         
                if (RdBtnOneError.IsChecked == true)
                {
                    ServiceSingleton.Settings.StoreIniValue("Process", "ErrorsThreshold", "1");
                }   
                else if (RdBtnNoThreshold.IsChecked == true)
                {
                    ServiceSingleton.Settings.StoreIniValue("Process", "ErrorsThreshold", "0");
                }
                else
                {
                    if (ServiceSingleton.Settings.ErrorsThreshold == 1 || ServiceSingleton.Settings.ErrorsThreshold == 0)
                    {
                        ServiceSingleton.Settings.StoreIniValue("Process", "ErrorsThreshold", "50");
                    }                    
                }               
                                                    
                ServiceSingleton.Dashboard.LoadFrameAsync<PackageFrame>();
            }
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            if (ApiManager.AccountInfo.IsPremium)
            {
                ServiceSingleton.Dashboard.LoadFrame<CDNFrame>();
            }
            else
            {
                ServiceSingleton.Dashboard.LoadFrame<PageFileFrame>();
            }
        }
    }
}