using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;
using Nolvus.Dashboard.Controls;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Nolvus.NexusApi;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Controls;

namespace Nolvus.Dashboard.Frames.Installer
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
                LblResolution.Text = Instance.SettingsManager.Width + "x" + Instance.SettingsManager.Height;
                LblInstallDir.Text = Instance.SettingsManager.InstallDir;
                LblArchiveDir.Text = Instance.SettingsManager.ArchiveDir;

                LblEnableArchiving.Text = Instance.SettingsManager.EnableArchiving ? "Yes" : "No";

                LblDownscaling.Text = Instance.SettingsManager.DownScaling == "TRUE" ? "Yes (" + Instance.SettingsManager.DownScaledResolution + ")" : "No";

                LblVariant.Text = Instance.SettingsManager.Variant;
                LblCDN.Text = Instance.SettingsManager.CDN;
                LblLanguage.Text = Instance.SettingsManager.LgName;

                switch (Instance.SettingsManager.IniSettings)
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

                // Avalonia's DataGrid groups through its collection view, and the groups start
                // expanded, which stands in for upstream's ExpandAllGroup().
                var Options = new DataGridCollectionView(Instance.OptionsManager.GetOptions());
                Options.GroupDescriptions.Add(new DataGridPathGroupDescription("Category"));

                OptionsGrid.ItemsSource = Options;

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
                    RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages.", 50);
                }
                else if (ServiceSingleton.Settings.ErrorsThreshold == 0)
                {
                    RdBtnNoThreshold.IsChecked = true;
                    RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages.", 50);
                }
                else
                {
                    RdBtnThreshold.IsChecked = true;
                    RdBtnThreshold.Content = string.Format("Stop the installation when {0} errors occured and display the error messages.", ServiceSingleton.Settings.ErrorsThreshold);
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
                NolvusMessageBox.Show(owner, "Global ModOrganizer instance detected", "The installer can not proceed to the installation because a global ModOrganizer instance has been detected. Read the message below to fix\n\nREAD THIS TO FIX!!!\n\nAll automated mod lists use portable instances, this way you can have multiple lists installed together.\n\nIf you want to install Nolvus, you need to remove this installed ModOrganizer global instance to avoid issues(make a backup before if it's sensitive).\n\nTo know where your global instance is installed go to \n\n" + Mo2Path + ".\n\nThis folder may be hidden (be sure you disable hidden files and folder in Windows folder options if you don't see it)\n\nIf you made a backup of your global instance and want to continue, just delete the folder\n\n" + Mo2Path + "\n\nDON'T REACTIVATE THIS GLOBAL INSTANCE AFTER INSTALLATION!!! YOUR NOLVUS MOD ORGANIZER WILL NOT WORK!!!", MessageBoxType.Error, 600, 620, Colors.White);
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
