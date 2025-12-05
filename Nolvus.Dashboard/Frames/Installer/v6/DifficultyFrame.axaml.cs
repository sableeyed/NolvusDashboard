using Avalonia.Controls;
using Avalonia.Interactivity;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class DifficultyFrame : DashboardFrame
    {
        public DifficultyFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            TglBtnBoss.Click += OnBossChanged;
            TglBtnExhaustion.Click += OnExhaustionChanged;
            BtnContinue.Click += BtnContinue_Click;
            BtnPrevious.Click += BtnPrevious_Click;
        }

        private int ScalingsIndex(List<string> Scalings)
        {            
            var Index = Scalings.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Options.CombatScaling);

            return Index == -1 ? 0 : Index;                            
        }

        private int NerfPAIndex(List<string> NerfPAs)
        {
            var Index = NerfPAs.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Options.NerfPA);

            return Index == -1 ? 0 : Index;
        }

        private bool CheckIfPrepareToDie()
        {            
            if (ServiceSingleton.Instances.WorkingInstance.Options.CombatScaling == "Hard" &&
                ServiceSingleton.Instances.WorkingInstance.Options.Exhaustion == "TRUE" &&
                ServiceSingleton.Instances.WorkingInstance.Options.NerfPA == "Player Only" &&
                ServiceSingleton.Instances.WorkingInstance.Options.Boss == "TRUE")
            {

                return true;
            }

            return false;
        }

        private bool CheckIfHackAndSash()
        {
            if (ServiceSingleton.Instances.WorkingInstance.Options.CombatScaling == "Easy" &&
                ServiceSingleton.Instances.WorkingInstance.Options.Exhaustion == "FALSE" &&
                ServiceSingleton.Instances.WorkingInstance.Options.NerfPA == "NPCs Only" &&
                ServiceSingleton.Instances.WorkingInstance.Options.Boss == "FALSE")
            {

                return true;
            }

            return false;
        }

        private bool CheckIfHMilkDrinker()
        {
            if (ServiceSingleton.Instances.WorkingInstance.Options.CombatScaling == "Very Easy" &&
                ServiceSingleton.Instances.WorkingInstance.Options.Exhaustion == "FALSE" &&
                ServiceSingleton.Instances.WorkingInstance.Options.NerfPA == "NPCs Only" &&
                ServiceSingleton.Instances.WorkingInstance.Options.Boss == "FALSE")
            {

                return true;
            }

            return false;
        }

        private bool CheckIfTrueNord()
        {

            if (ServiceSingleton.Instances.WorkingInstance.Options.CombatScaling == "Medium" &&
                ServiceSingleton.Instances.WorkingInstance.Options.Exhaustion == "TRUE" &&
                ServiceSingleton.Instances.WorkingInstance.Options.NerfPA == "Both" &&
                ServiceSingleton.Instances.WorkingInstance.Options.Boss == "TRUE")                
            {

                return true;
            }

            return false;
        }

        protected override async Task OnLoadedAsync()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            List<string> Presets = new List<string>();

            Presets.Add("Milk Drinker");
            Presets.Add("Hack and Slash");
            Presets.Add("True Nord");
            Presets.Add("Prepare to Die");

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
            else if (CheckIfHMilkDrinker())
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

            List<string> CombatScalings = new List<string>();

            CombatScalings.Add("Very Easy");
            CombatScalings.Add("Easy");
            CombatScalings.Add("Medium");
            CombatScalings.Add("Hard");

            DrpDwnLstCombatScaling.ItemsSource = CombatScalings;

            DrpDwnLstCombatScaling.SelectedIndex = ScalingsIndex(CombatScalings);

            TglBtnExhaustion.IsChecked = false;

            if (Instance.Options.Exhaustion == "TRUE")
            {
                TglBtnExhaustion.IsChecked = true;
            }      

            List<string> NerfPAs = new List<string>();

            NerfPAs.Add("None");
            NerfPAs.Add("Player Only");
            NerfPAs.Add("NPCs Only");
            NerfPAs.Add("Both");

            DrpDwnLstNerfPA.ItemsSource = NerfPAs;

            DrpDwnLstNerfPA.SelectedIndex = NerfPAIndex(NerfPAs);

            TglBtnBoss.IsChecked = false;

            if (Instance.Options.Boss == "TRUE")
            {
                TglBtnBoss.IsChecked = true;
            }

            ServiceSingleton.Dashboard.Info("Difficulty options");
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<v6.OptionsFrame>();
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", "The options you selected can not be changed after installation. Are you sure you want to continue?");
            if(result == true)
            {
                ServiceSingleton.Dashboard.LoadFrame<v6.ENBFrame>();
            }
        }

        private void OnPresetChanged(object? sender, SelectionChangedEventArgs e)
        {
            TglBtnBoss.IsEnabled = true;

            if (DrpDwnLstPreset.SelectedIndex == 3)
            {                
                DrpDwnLstCombatScaling.SelectedIndex = 3;
                TglBtnExhaustion.IsChecked = true;                
                DrpDwnLstNerfPA.SelectedIndex = 1;
                TglBtnBoss.IsChecked = true;             
            }
            else if (DrpDwnLstPreset.SelectedIndex == 2)
            {
                DrpDwnLstCombatScaling.SelectedIndex = 2;
                TglBtnExhaustion.IsChecked = true;                
                DrpDwnLstNerfPA.SelectedIndex = 3;
                TglBtnBoss.IsChecked = true;
            }
            else if (DrpDwnLstPreset.SelectedIndex == 1)
            {
                DrpDwnLstCombatScaling.SelectedIndex = 1;
                TglBtnExhaustion.IsChecked = false;                
                DrpDwnLstNerfPA.SelectedIndex = 2;
                TglBtnBoss.IsChecked = false;
            }
            else
            {
                DrpDwnLstCombatScaling.SelectedIndex = 0;
                TglBtnExhaustion.IsChecked = false;
                DrpDwnLstNerfPA.SelectedIndex = 2;
                TglBtnBoss.IsChecked = false;
                TglBtnBoss.IsEnabled = false;
            }
        }

        private void OnExhaustionChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnExhaustion.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Exhaustion = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Exhaustion = "FALSE";
            }
        }

        private void OnBossChanged(object? sender, RoutedEventArgs e)
        {
            if (TglBtnBoss.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Boss = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Boss = "FALSE";
            }
        }

        private void OnScalingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstCombatScaling.SelectedItem is string value)
                ServiceSingleton.Instances.WorkingInstance.Options.CombatScaling = value;
        }

        private void OnPAChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpDwnLstNerfPA.SelectedItem is string value)
                ServiceSingleton.Instances.WorkingInstance.Options.NerfPA = value;
        }
    }
}