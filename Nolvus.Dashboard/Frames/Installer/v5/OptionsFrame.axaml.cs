using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Controls;
using Nolvus.Dashboard.Controls;

namespace Nolvus.Dashboard.Frames.Installer.v5
{
    public partial class OptionsFrame : DashboardFrame
    {
        public OptionsFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();

            BtnContinue.Click += BtnContinue_Click;
            BtnPrevious.Click += BtnPrevious_Click;

            TglEnableNudity.IsCheckedChanged += OnNudityChanged;
            TglHardcoreMode.IsCheckedChanged += OnHardcoreChanged;
            TglAlternateLeveling.IsCheckedChanged += OnAlternateLevelingChanged;
            TglAlternateStart.IsCheckedChanged += OnAlternateStartChanged;
            TglFantasyMode.IsCheckedChanged += OnFantasyModeChanged;
        }

        private int SkinTypeIndex(IEnumerable<INolvusOptionChoiceDTO> SkinTypes)
        {
            var Index = SkinTypes.ToList().FindIndex(x => x.Value == ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOptionValue("SkinType"));
            return Index == -1 ? 0 : Index;   
        }

        protected override async Task OnLoadedAsync()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            TglEnableNudity.IsChecked = false;

            if (Instance.OptionsManager.GetOptionValue("Nudity") == "TRUE")
            {
                TglEnableNudity.IsChecked = true;
            }

            TglHardcoreMode.IsChecked = false;

            if (Instance.OptionsManager.GetOptionValue("HardcoreMode") == "TRUE")
            {
                TglHardcoreMode.IsChecked = true;
            }

            TglAlternateLeveling.IsChecked = false;

            if (Instance.OptionsManager.GetOptionValue("AlternateLeveling") == "TRUE")
            {
                TglAlternateLeveling.IsChecked = true;
            }

            TglAlternateStart.IsChecked = false;

            if (Instance.OptionsManager.GetOptionValue("AlternateStart") == "TRUE")
            {
                TglAlternateStart.IsChecked = true;
            }

            TglFantasyMode.IsChecked = false;

            if (Instance.OptionsManager.GetOptionValue("FantasyMode") == "TRUE")
            {
                TglFantasyMode.IsChecked = true;
            }

            var SkinTypes = await ApiManager.Service.Installer.GetOptionChoices("SkinType", Instance.SettingsManager.Variant, Instance.Id);

            DrpFemaleSkinType.ItemsSource = SkinTypes;

            DrpFemaleSkinType.SelectedIndex = SkinTypeIndex(SkinTypes);

            ServiceSingleton.Dashboard.Info("Additional Options");
        }

        private async void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            bool? result = await NolvusMessageBox.ShowConfirmation(owner, "Confirmation", "The options you selected can not be changed after installation. Are you sure you want to continue?");
            if (result == true)
            {
                ServiceSingleton.Dashboard.LoadFrame<v5.ENBFrame>();
            }
            else 
            {
                return;
            }
        }

        private async void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<v5.PerformanceFrame>();
        }

        private void OnNudityChanged(object? sender, RoutedEventArgs e)
        {
            if (TglEnableNudity.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Nudity").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("Nudity").Value = "FALSE";
            }
        }

        private void OnSkinTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("SkinType").Value = (DrpFemaleSkinType.SelectedItem as INolvusOptionChoiceDTO).Value;
        }

        private void OnHardcoreChanged(object? sender, RoutedEventArgs e)
        {
            if (TglHardcoreMode.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("HardcoreMode").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("HardcoreMode").Value = "FALSE";
            }
        }

        private void OnAlternateLevelingChanged(object? sender, RoutedEventArgs e)
        {
            if (TglAlternateLeveling.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AlternateLeveling").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AlternateLeveling").Value = "FALSE";
            }
        }

        private void OnAlternateStartChanged(object? sender, RoutedEventArgs e)
        {
            if (TglAlternateStart.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AlternateStart").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("AlternateStart").Value = "FALSE";
            }
        }

        private void OnFantasyModeChanged(object? sender, RoutedEventArgs e)
        {
            if (TglFantasyMode.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("FantasyMode").Value = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.OptionsManager.GetOption("FantasyMode").Value = "FALSE";
            }
        }

    }
}