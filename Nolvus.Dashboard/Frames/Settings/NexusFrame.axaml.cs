using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames;
using Nolvus.Dashboard.Core;

namespace Nolvus.Dashboard.Frames.Settings
{
    public partial class NexusFrame : DashboardFrame
    {
        public NexusFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();

            BtnAuthenticate.Click += BtnAuthenticate_Click;
            BtnPrevious.Click += BtnPrevious_Click;
            BtnNext.Click += BtnNext_Click;

            // Disable settings until authenticated
            (TopLevel.GetTopLevel(this) as DashboardWindow)?.DisableSettings();

            UpdateNextButtonState();
        }

        private void BtnPrevious_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Dashboard.LoadFrame<GameFrame>();
        }

        private void BtnNext_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;

            if (string.IsNullOrWhiteSpace(SettingsCache.NexusApiKey))
            {
                _ = NolvusMessageBox.Show(owner,
                    "Nexus Login Required",
                    "Please authenticate using Nexus SSO before continuing.",
                    MessageBoxType.Warning);
                return;
            }

            //ServiceSingleton.Dashboard.LoadFrame<NolvusFrame>();
        }

        private async void BtnAuthenticate_Click(object? sender, RoutedEventArgs e)
        {
            var owner = TopLevel.GetTopLevel(this) as Window;

            Nolvus.Browser.Browser.OpenBrowser("https://nexusmods.com");

            // TODO: Replace with actual WebView authentication
            //SettingsCache.NexusApiKey = "SIMULATED-TEST-KEY";

            // await NolvusMessageBox.Show(owner,
            //     "Authentication Success",
            //     "You are now authenticated! Click Next to continue.",
            //     MessageBoxType.Info);

            // UpdateNextButtonState();
        }

        private void UpdateNextButtonState()
        {
            BtnNext.IsEnabled = !string.IsNullOrWhiteSpace(SettingsCache.NexusApiKey);
        }
    }
}
