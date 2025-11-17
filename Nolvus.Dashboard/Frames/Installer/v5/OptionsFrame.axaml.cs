using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
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

        private int SkinTypeIndex(List<string> SkinTypes)
        {
            var Index = SkinTypes.FindIndex(x => x == ServiceSingleton.Instances.WorkingInstance.Options.SkinType);
            return Index == -1 ? 0 : Index;   
        }

        protected override async Task OnLoadedAsync()
        {
            var Instance = ServiceSingleton.Instances.WorkingInstance;

            TglEnableNudity.IsChecked = false;

            if (Instance.Options.Nudity == "TRUE")
            {
                TglEnableNudity.IsChecked = true;
            }

            TglHardcoreMode.IsChecked = false;

            if (Instance.Options.HardcoreMode == "TRUE")
            {
                TglHardcoreMode.IsChecked = true;
            }

            TglAlternateLeveling.IsChecked = false;

            if (Instance.Options.AlternateLeveling == "TRUE")
            {
                TglAlternateLeveling.IsChecked = true;
            }

            TglAlternateStart.IsChecked = false;

            if (Instance.Options.AlternateStart == "TRUE")
            {
                TglAlternateStart.IsChecked = true;
            }

            TglFantasyMode.IsChecked = false;

            if (Instance.Options.FantasyMode == "TRUE")
            {
                TglFantasyMode.IsChecked = true;
            }

            List<string> SkinTypes = new List<string>();
            SkinTypes.Add("Smooth");
            SkinTypes.Add("Muscular");

            DrpFemaleSkinType.ItemsSource = SkinTypes;

            DrpFemaleSkinType.SelectedIndex = SkinTypeIndex(SkinTypes);

            ServiceSingleton.Dashboard.Info("Additional Options");
        }

        private void BtnContinue_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            NolvusMessageBox.Show(owner, "Error", "Unimplemented - do not report as a bug", MessageBoxType.Error);
            return;
        }

        private async void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<v5.PerformanceFrame>();
        }

        private void OnNudityChanged(object? sender, RoutedEventArgs e)
        {
            if (TglEnableNudity.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Nudity = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.Nudity = "FALSE";
            }
        }

        private void OnSkinTypeChanged(object? sender, SelectionChangedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance.Options.SkinType = DrpFemaleSkinType.SelectedItem.ToString();
        }

        private void OnHardcoreChanged(object? sender, RoutedEventArgs e)
        {
            if (TglHardcoreMode.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.HardcoreMode = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.HardcoreMode = "FALSE";
            }
        }

        private void OnAlternateLevelingChanged(object? sender, RoutedEventArgs e)
        {
            if (TglAlternateLeveling.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.AlternateLeveling = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.AlternateLeveling = "FALSE";
            }
        }

        private void OnAlternateStartChanged(object? sender, RoutedEventArgs e)
        {
            if (TglAlternateStart.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.AlternateStart = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.AlternateStart = "FALSE";
            }
        }

        private void OnFantasyModeChanged(object? sender, RoutedEventArgs e)
        {
            if (TglFantasyMode.IsChecked == true)
            {
                ServiceSingleton.Instances.WorkingInstance.Options.FantasyMode = "TRUE";
            }
            else
            {
                ServiceSingleton.Instances.WorkingInstance.Options.FantasyMode = "FALSE";
            }
        }

    }
}