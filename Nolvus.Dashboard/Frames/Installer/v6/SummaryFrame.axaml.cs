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

                LblNudity.Text = Instance.Options.Nudity == "TRUE" ? "Yes" : "No";
                LblCombatAnims.Text = Instance.Options.CombatAnimation;
                LblLeveling.Text = Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No";
                LblAltStart.Text = Instance.Options.AlternateStart == "TRUE" ? "Yes" : "No";
                LblGore.Text = Instance.Options.Gore == "TRUE" ? "Yes" : "No";
                LblStancesPerksTree.Text = Instance.Options.StancesPerksTree == "TRUE" ? "Yes" : "No";
                LblENB.Text = ENBs.GetENBByCode(Instance.Options.AlternateENB);

                LblCombatScaling.Text = Instance.Options.CombatScaling;
                LblExhaustion.Text = Instance.Options.Exhaustion == "TRUE" ? "Yes" : "No";
                LblNerfPA.Text = Instance.Options.NerfPA;
                LblEnemiesResistance.Text = Instance.Options.EnemiesResistance == "TRUE" ? "Yes" : "No";
                LblBoss.Text = Instance.Options.Boss == "TRUE" ? "Yes" : "No";
                LblPoise.Text = Instance.Options.Poise == "TRUE" ? "Yes" : "No";
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
                    
                }
                else if (ServiceSingleton.Settings.ErrorsThreshold == 0)
                {
                    RdBtnNoThreshold.IsChecked = true;
                }
                else
                {
                    RdBtnThreshold.IsChecked = true;
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
            
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            
        }
    }
}