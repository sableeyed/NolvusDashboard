using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Nolvus.Dashboard.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using Avalonia.Data;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;

namespace Nolvus.Dashboard.Frames.Installer.v6
{
    public partial class OptionsFrame : DashboardFrame
    {
        public OptionsFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            BtnPrevious.Click += BtnPrevious_Click;
            BtnContinue.Click += BtnContinue_Click;
            TglNudity.IsCheckedChanged += OnNudityChanged;
            TglLeveling.IsCheckedChanged += OnLevelingChanged;
            TglGore.IsCheckedChanged += OnGoreChanged;
            TglController.IsCheckedChanged += OnControllerChanged;
            BtnShowUI.Click += BtnShowUI_Click;
            BtnShowGore.Click += BtnShowGore_Click;
            BtnShowController.Click += BtnShowController_Click;
        }

        private int AnimsIndex(IEnumerable<INolvusOptionChoiceDTO> Anims)
        {            
            var Index = Anims.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("CombatAnimation"));

            return Index == -1 ? 0 : Index;                            
        }

        private int UIsIndex(IEnumerable<INolvusOptionChoiceDTO> UIs)
        {
            var Index = UIs.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("UI"));

            return Index == -1 ? 0 : Index;
        }

        protected override async Task OnLoadedAsync()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            LblGOWarning.IsVisible = Instance.SettingsManager.Variant == Strings.GO;

            TglNudity.IsChecked = false;

            TglLeveling.IsChecked = false;
            TglGore.IsChecked = false;
            TglController.IsChecked = false;

            TglLeveling.IsEnabled = Instance.SettingsManager.Variant != Strings.GO;
            TglGore.IsEnabled = Instance.SettingsManager.Variant != Strings.GO;
            TglController.IsEnabled = Instance.SettingsManager.Variant != Strings.GO;

            LblALNotUsed.IsVisible = Instance.SettingsManager.Variant == Strings.GO;
            LblCANotUsed.IsVisible = Instance.SettingsManager.Variant == Strings.GO;
            LblGoreNotUsed.IsVisible = Instance.SettingsManager.Variant == Strings.GO;
            LblCSNotUsed.IsVisible = Instance.SettingsManager.Variant == Strings.GO;

            DrpCombat.IsEnabled = Instance.SettingsManager.Variant != Strings.GO;

            if (Instance.OptionsManager.GetOptionValue("Nudity") == "TRUE")
            {
                TglNudity.IsChecked = true;
            }

            if (Instance.SettingsManager.Variant != Strings.GO)
            {
                if (Instance.OptionsManager.GetOptionValue("AlternateLeveling") == "TRUE")
                {
                    TglLeveling.IsChecked = true;
                }

                if (Instance.OptionsManager.GetOptionValue("Gore") == "TRUE")
                {
                    TglGore.IsChecked = true;
                }

                if (Instance.OptionsManager.GetOptionValue("Controller") == "TRUE")
                {
                    TglController.IsChecked = true;
                }

                var CombatAnims = await ApiManager.Service.Installer.GetOptionChoices("CombatAnimation", Instance.SettingsManager.Variant, Instance.Id);

                DrpCombat.ItemsSource = CombatAnims;
                DrpCombat.DisplayMemberBinding = new Binding("Value");

                DrpCombat.SelectedIndex = AnimsIndex(CombatAnims);
            }
            else
            {
                var GOCombatAnims = new List<string>();
                GOCombatAnims.Add("Not used");
                DrpCombat.ItemsSource = GOCombatAnims;
                DrpCombat.SelectedIndex = 0;
            }

            var UIs = await ApiManager.Service.Installer.GetOptionChoices("UI", Instance.SettingsManager.Variant, Instance.Id);

            DrpUI.ItemsSource = UIs;

            DrpUI.SelectedIndex = UIsIndex(UIs);

            ServiceSingleton.Dashboard.Info("Options");
        }

        private async void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<v6.PerformanceFrame>();
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.Variant != Strings.GO)
            {
                await ServiceSingleton.Dashboard.LoadFrameAsync<v6.DifficultyFrame>();
            }
            else
            {
                ServiceSingleton.Dashboard.LoadFrame<v6.ENBFrame>();
            }
        }

        private void OnNudityChanged(object? sender, RoutedEventArgs e)
        {
            if (TglNudity.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Nudity").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Nudity").Value = "FALSE";
            }
        }

        private void OnLevelingChanged(object? sender, RoutedEventArgs e)
        {
            if (TglLeveling.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AlternateLeveling").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AlternateLeveling").Value = "FALSE";
            }
        }

        private void OnGoreChanged(object? sender, RoutedEventArgs e)
        {
            if (TglGore.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Gore").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Gore").Value = "FALSE";
            }
        }

        private void OnAnimationsChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ServiceSingleton.Instances.WorkingInstance.SettingsManager.Variant != Strings.GO)
            {
                if (DrpCombat.SelectedItem is INolvusOptionChoiceDTO Choice)
                    ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("CombatAnimation").Value = Choice.Value;
            }
        }

        private void OnUIChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DrpUI.SelectedItem is INolvusOptionChoiceDTO Choice)
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("UI").Value = Choice.Value;
        }

        private void OnControllerChanged(object? sender, RoutedEventArgs e)
        {
            if (TglController.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Controller").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Controller").Value = "FALSE";
            }
        }

        private void BtnShowUI_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/ui/choices") { UseShellExecute = true });
        }

        private void BtnShowAlternateLeveling_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/game/leveling") { UseShellExecute = true });
        }

        private void BtnShowGore_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/combat/gore") { UseShellExecute = true });
        }

        private void BtnShowController_Click(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide/keybinds/controller") { UseShellExecute = true });
        }

    }
}