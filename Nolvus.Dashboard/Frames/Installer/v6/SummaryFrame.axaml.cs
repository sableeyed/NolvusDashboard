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
using Avalonia.Controls;
using Nolvus.Dashboard.Controls;

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
                LblCombatAnims.Text = string.Format("{0} {1}", Instance.Options.CombatAnimation, Instance.Performance.Variant == Strings.GO ? " (Not used)" : string.Empty);
                LblLeveling.Text = string.Format("{0} {1}", Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No", Instance.Performance.Variant == Strings.GO ? " (Not used)" : string.Empty);
                LblGore.Text = string.Format("{0} {1}", Instance.Options.Gore == "TRUE" ? "Yes" : "No", Instance.Performance.Variant == Strings.GO ? " (Not used)" : string.Empty);
                LblENB.Text = ENBs.GetENBByCode(Instance.Options.AlternateENB);

                LblCombatScaling.Text = string.Format("{0} {1}", Instance.Options.CombatScaling, Instance.Performance.Variant == Strings.GO ? " (Not used)" : string.Empty);
                LblExhaustion.Text = string.Format("{0} {1}", Instance.Options.Exhaustion == "TRUE" ? "Yes" : "No", Instance.Performance.Variant == Strings.GO ? " (Not used)" : string.Empty);
                LblNerfPA.Text = string.Format("{0} {1}", Instance.Options.NerfPA, Instance.Performance.Variant == Strings.GO ? " (Not used)" : string.Empty);
                LblBoss.Text = string.Format("{0} {1}", Instance.Options.Boss == "TRUE" ? "Yes" : "No", Instance.Performance.Variant == Strings.GO ? " (Not used)" : string.Empty);
                LblController.Text = string.Format("{0} {1}", Instance.Options.Controller == "TRUE" ? "Yes" : "No", Instance.Performance.Variant == Strings.GO ? " (Not used)" : string.Empty);
                LblUI.Text = Instance.Options.UI;

                LblCombatAnims.Foreground = Instance.Performance.Variant == Strings.GO ? new SolidColorBrush(Colors.Silver) : new SolidColorBrush(Color.FromRgb(242,143,26));
                LblLeveling.Foreground = Instance.Performance.Variant == Strings.GO ? new SolidColorBrush(Colors.Silver) : new SolidColorBrush(Color.FromRgb(242,143,26));
                LblGore.Foreground = Instance.Performance.Variant == Strings.GO ? new SolidColorBrush(Colors.Silver) : new SolidColorBrush(Color.FromRgb(242,143,26));
                LblCombatScaling.Foreground = Instance.Performance.Variant == Strings.GO ? new SolidColorBrush(Colors.Silver) : new SolidColorBrush(Color.FromRgb(242,143,26));
                LblExhaustion.Foreground = Instance.Performance.Variant == Strings.GO ? new SolidColorBrush(Colors.Silver) : new SolidColorBrush(Color.FromRgb(242,143,26));
                LblNerfPA.Foreground = Instance.Performance.Variant == Strings.GO ? new SolidColorBrush(Colors.Silver) : new SolidColorBrush(Color.FromRgb(242,143,26));
                LblBoss.Foreground = Instance.Performance.Variant == Strings.GO ? new SolidColorBrush(Colors.Silver) : new SolidColorBrush(Color.FromRgb(242,143,26));
                LblController.Foreground = Instance.Performance.Variant == Strings.GO ? new SolidColorBrush(Colors.Silver) : new SolidColorBrush(Color.FromRgb(242,143,26));

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
                var owner = TopLevel.GetTopLevel(this) as Window;        
                NolvusMessageBox.Show(owner, "Global ModOrganizer instance detected", "The installer can not proceed to the installation because a global ModOrganizer instance has been detected. Read the message below to fix\n\nREAD THIS TO FIX!!!\n\nAll automated mod lists use portable instances, this way you can have multiple lists installed together.\n\nIf you want to install Nolvus, you need to remove this installed ModOrganizer global instance to avoid issues(make a backup before if it's sensitive).\n\nTo know where your global instance is installed go to \n\n" + Mo2Path + ".\n\nThis folder may be hidden (be sure you disable hidden files and folder in Windows folder options if you don't see it)\n\nIf you made a backup of your global instance and want to continue, just delete the folder\n\n" + Mo2Path + "\n\nDON'T REACTIVATE THIS GLOBAL INSTANCE AFTER INSTALLATION!!! YOUR NOLVUS MOD ORGANIZER WILL NOT WORK!!!", MessageBoxType.Error, 600, 620, Color.FromRgb(0, 0, 0));
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