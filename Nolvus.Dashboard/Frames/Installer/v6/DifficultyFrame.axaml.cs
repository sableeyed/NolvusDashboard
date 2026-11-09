using Avalonia.Controls;
using Avalonia.Interactivity;
using Nolvus.Core.Misc;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Controls;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using System.Diagnostics;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class DifficultyFrame : DashboardFrame
    {
        public DifficultyFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            TglBtnBoss.IsCheckedChanged += OnBossChanged;
            TglBtnExhaustion.IsCheckedChanged += OnExhaustionChanged;
            TglBtnZoneLevel.IsCheckedChanged += OnZoneLevelChanged;
            BtnContinue.Click += BtnContinue_Click;
            BtnPrevious.Click += BtnPrevious_Click;
            BtnPresetsExplained.Click += BtnPresetsExplained_Click;
            BtnScalingExplained.Click += BtnScalingExplained_Click;
            BtnZoneLevelExplained.Click += BtnZoneLevelExplained_Click;
        }

        private int ScalingsIndex(IEnumerable<INolvusOptionChoiceDTO> Scalings)
        {
            var Index = Scalings.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("CombatScaling"));

            return Index == -1 ? 0 : Index;
        }

        private int NerfPAIndex(IEnumerable<INolvusOptionChoiceDTO> NerfPAs)
        {
            var Index = NerfPAs.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("NerfPA"));

            return Index == -1 ? 0 : Index;
        }

        private bool CheckIfPrepareToDie()
        {
            if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("CombatScaling") == "Hard" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Exhaustion") == "TRUE" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("NerfPA") == "Player Only" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Boss") == "TRUE")
            {

                return true;
            }

            return false;
        }

        private bool CheckIfHackAndSash()
        {
            if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("CombatScaling") == "Easy" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Exhaustion") == "FALSE" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("NerfPA") == "NPCs Only" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Boss") == "FALSE")
            {

                return true;
            }

            return false;
        }

        private bool CheckIfMilkDrinker()
        {
            if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("CombatScaling") == "Very Easy" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Exhaustion") == "FALSE" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("NerfPA") == "NPCs Only" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Boss") == "FALSE")
            {

                return true;
            }

            return false;
        }

        private bool CheckIfTrueNord()
        {

            if (ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("CombatScaling") == "Medium" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Exhaustion") == "TRUE" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("NerfPA") == "Both" &&
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("Boss") == "TRUE")
            {

                return true;
            }

            return false;
        }

        protected override async Task OnLoadedAsync()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            TglBtnBoss.IsChecked = false;
            TglBtnExhaustion.IsChecked = false;
            TglBtnZoneLevel.IsChecked = false;

            List<string> Presets = new List<string>();

            Presets.Add("Milk Drinker");
            Presets.Add("Hack and Slash");
            Presets.Add("True Nord");
            Presets.Add("Prepare to Die");

            DrpDwnLstPreset.SelectionChanged -= OnPresetChanged;

            if (CheckIfPrepareToDie())
            {
                DrpDwnLstPreset.ItemsSource = Presets;
                DrpDwnLstPreset.SelectedIndex = 3;
            }
            else if (CheckIfTrueNord())
            {
                DrpDwnLstPreset.ItemsSource = Presets;
                DrpDwnLstPreset.SelectedIndex = 2;
            }
            else if (CheckIfHackAndSash())
            {
                DrpDwnLstPreset.ItemsSource = Presets;
                DrpDwnLstPreset.SelectedIndex = 1;
            }
            else if (CheckIfMilkDrinker())
            {
                DrpDwnLstPreset.ItemsSource = Presets;
                DrpDwnLstPreset.SelectedIndex = 0;
            }
            else
            {
                Presets.Add("Customized");
                DrpDwnLstPreset.ItemsSource = Presets;
                DrpDwnLstPreset.SelectedIndex = 4;
            }

            DrpDwnLstPreset.SelectionChanged += OnPresetChanged;

            var CombatScalings = await ApiManager.Service.Installer.GetOptionChoices("CombatScaling", Instance.SettingsManager.Variant, Instance.Id);

            DrpDwnLstCombatScaling.ItemsSource = CombatScalings;

            DrpDwnLstCombatScaling.SelectedIndex = ScalingsIndex(CombatScalings);

            var NerfPAs = await ApiManager.Service.Installer.GetOptionChoices("NerfPA", Instance.SettingsManager.Variant, Instance.Id);

            DrpDwnLstNerfPA.ItemsSource = NerfPAs;

            DrpDwnLstNerfPA.SelectedIndex = NerfPAIndex(NerfPAs);

            if (Instance.OptionsManager.GetOptionValue("Boss") == "TRUE")
            {
                TglBtnBoss.IsChecked = true;
            }

            if (Instance.OptionsManager.GetOptionValue("Exhaustion") == "TRUE")
            {
                TglBtnExhaustion.IsChecked = true;
            }

            if (Instance.OptionsManager.GetOptionValue("ZoneLevelIndicator") == "TRUE")
            {
                TglBtnZoneLevel.IsChecked = true;
            }

            ServiceSingleton.Dashboard.Info("Difficulty options");
        }

        private async void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<v6.OptionsFrame>();
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<v6.ENBFrame>();
        }

        private void OnPresetChanged(object? sender, SelectionChangedEventArgs e)
        {
            TglBtnBoss.IsEnabled = true;

            if (DrpDwnLstPreset.SelectedIndex == 3)
            {
                //P2D
                DrpDwnLstCombatScaling.SelectedIndex = 3;
                TglBtnExhaustion.IsChecked = true;
                DrpDwnLstNerfPA.SelectedIndex = 1;
                TglBtnBoss.IsChecked = true;
            }
            else if (DrpDwnLstPreset.SelectedIndex == 2)
            {
                //True Nord
                DrpDwnLstCombatScaling.SelectedIndex = 2;
                TglBtnExhaustion.IsChecked = true;
                DrpDwnLstNerfPA.SelectedIndex = 3;
                TglBtnBoss.IsChecked = true;
            }
            else if (DrpDwnLstPreset.SelectedIndex == 1)
            {
                //Hack & Slash
                DrpDwnLstCombatScaling.SelectedIndex = 1;
                TglBtnExhaustion.IsChecked = false;
                DrpDwnLstNerfPA.SelectedIndex = 2;
                TglBtnBoss.IsChecked = false;
            }
            else
            {
                //Milk Drinker
                DrpDwnLstCombatScaling.SelectedIndex = 0;
                TglBtnExhaustion.IsChecked = false;
                DrpDwnLstNerfPA.SelectedIndex = 2;
                TglBtnBoss.IsChecked = false;
                TglBtnBoss.IsEnabled = false;
            }
        }

        private void OnBossChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnBoss.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Boss").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Boss").Value = "FALSE";
            }
        }

        private void OnExhaustionChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnExhaustion.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Exhaustion").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Exhaustion").Value = "FALSE";
            }
        }

        private void OnZoneLevelChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnZoneLevel.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("ZoneLevelIndicator").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("ZoneLevelIndicator").Value = "FALSE";
            }
        }

        private void OnScalingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstCombatScaling.SelectedItem is INolvusOptionChoiceDTO Choice)
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("CombatScaling").Value = Choice.Value;
        }

        private void OnPAChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstNerfPA.SelectedItem is INolvusOptionChoiceDTO Choice)
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("NerfPA").Value = Choice.Value;
        }

        private void BtnPresetsExplained_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/combat/presets") { UseShellExecute = true });
        }

        private void BtnScalingExplained_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/combat/scaling") { UseShellExecute = true });
        }

        private void BtnZoneLevelExplained_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/combat/scaling") { UseShellExecute = true });
        }
    }
}
