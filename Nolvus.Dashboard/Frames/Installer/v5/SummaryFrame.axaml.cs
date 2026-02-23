using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;
using Nolvus.Dashboard.Controls;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Nolvus.NexusApi;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Controls;

namespace Nolvus.Dashboard.Frames.Installer.v5
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

                LblInstanceName.Text = Instance.Name;
                LblResolution.Text = Instance.Settings.Width + "x" + Instance.Settings.Height;
                LblScreenRatio.Text = Instance.Settings.Ratio;
                LblInstallDir.Text = Instance.InstallDir;
                LblArchiveDir.Text = Instance.ArchiveDir;

                LblEnableArchiving.Text = Instance.Settings.EnableArchiving ? "Yes" : "No";

                LblDownscaling.Text = Instance.Performance.DownScaling == "TRUE" ? "Yes (" + Instance.Performance.DownScaledResolution + ")" : "No";
                LblVariant.Text = Instance.Performance.Variant;
                LblAntiAliasing.Text = Instance.Performance.AntiAliasing;

                switch (Instance.Performance.IniSettings)
                {
                    case "0": LblIniProfile.Text = "Low";
                        break;
                    case "1": LblIniProfile.Text = "Medium";
                        break;
                    case "2": LblIniProfile.Text = "High";
                        break;
                }

                LblAdvancedPhysics.Text = Instance.Performance.AdvancedPhysics == "TRUE" ? "Yes" : "No";
                LblLods.Text = Instance.Performance.LODs;
                LblRayTracing.Text = Instance.Performance.RayTracing == "TRUE" ? "Yes" : "No";
                LblFpsStabilizer.Text = Instance.Performance.FPSStabilizer == "TRUE" ? "Yes" : "No";

                LblNudity.Text = Instance.Options.Nudity == "TRUE" ? "Yes" : "No";
                LblHardcoreMode.Text = Instance.Options.HardcoreMode == "TRUE" ? "Yes" : "No";
                LblAlternateLeveling.Text = Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No";
                LblAlternateStart.Text = Instance.Options.AlternateStart == "TRUE" ? "Yes" : "No";
                LblSkinType.Text = Instance.Options.SkinType;
                LblFantasyMode.Text = Instance.Options.FantasyMode == "TRUE" ? "Yes" : "No";
                LblENB.Text = ENBs.GetENBByCode(Instance.Options.AlternateENB);


                if (!ApiManager.AccountInfo.IsPremium)
                {
                    NotificationBanner.Background = new SolidColorBrush(Colors.Orange);
                    LblNotification.Text = "You are not a Nexus Premium user. Download will not be automatic (you will have to click the download button for each mod) and bandwidth will be limited to 2 MB/s";
                    PremiumImg.IsVisible = false;
                    NonPremiumImg.IsVisible = true;
                }
                //no need for an else because the default state of the UI components are for premium members

                if (ServiceSingleton.Settings.ErrorsThreshold == 1 || !NexusApi.ApiManager.AccountInfo.IsPremium)
                {
                    RdoStopOnFirstError.IsChecked = true;
                    
                }
                else if (ServiceSingleton.Settings.ErrorsThreshold == 0)
                {
                    RdoDontStop.IsChecked = true;
                }
                else
                {
                    RdoStopOn50Errors.IsChecked = true;
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

            //MO2 is not native on linux so this should be irrelevant
            string Mo2Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ModOrganizer");

            if (Directory.Exists(Mo2Path))
            {
                var owner = TopLevel.GetTopLevel(this) as Window;
                NolvusMessageBox.Show(owner, "Global ModOrganizer instance detected", "The installer can not proceed to the installation because a global ModOrganizer instance has been detected. Read the message below to fix\n\nREAD THIS TO FIX!!!\n\nAll automated mod lists use portable instances, this way you can have multiple lists installed together.\n\nIf you want to install Nolvus, you need to remove this installed ModOrganizer global instance to avoid issues(make a backup before if it's sensitive).\n\nTo know where your global instance is installed go to \n\n" + Mo2Path + ".\n\nThis folder may be hidden (be sure you disable hidden files and folder in Windows folder options if you don't see it)\n\nIf you made a backup of your global instance and want to continue, just delete the folder\n\n" + Mo2Path + "\n\nDON'T REACTIVATE THIS GLOBAL INSTANCE AFTER INSTALLATION!!! YOUR NOLVUS MOD ORGANIZER WILL NOT WORK!!!", MessageBoxType.Error, 500, 520, Color.FromRgb(0, 0, 0));
            }
            else
            {
                if (RdoStopOnFirstError.IsChecked == true)
                {
                    ServiceSingleton.Settings.StoreIniValue("Process", "ErrorsThreshold", "1");
                }
                else if (RdoDontStop.IsChecked == true)
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