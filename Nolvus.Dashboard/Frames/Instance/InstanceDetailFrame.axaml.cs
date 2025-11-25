using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Enums;
using Nolvus.Core.Services;
using Nolvus.Package.Mods;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames.Installer;
using Nolvus.Dashboard.Frames.Instance.v5;
using Nolvus.Core.Misc;

namespace Nolvus.Dashboard.Frames.Instance
{
    public partial class InstanceDetailFrame : DashboardFrame
    {
        private ModObjectList ModListStatus = new ModObjectList();
        private string _CurrentProfile;

        public InstanceDetailFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();

            // Wire buttons
            BtnBack.Click += BtnBack_Click;
            BtnPlay.Click += BtnPlay_Click;
            BtnSettings.Click += BtnSettings_Click;
            BtnLoadOrder.Click += BtnLoadOrder_Click;
            BtnReport.Click += BtnReport_Click;

            DrpDwnLstProfiles.SelectionChanged += DrpDwnLstProfiles_SelectionChanged;

            // Loading overlay hidden initially
            PicBoxLoading.IsVisible = false;
        }

        protected override async Task OnLoadedAsync()
        {
            var instance = ServiceSingleton.Instances.WorkingInstance;

            LblHeader.Text = $"{instance.Name} v{instance.Version}";
            ServiceSingleton.Dashboard.Info($"Instance mods for {instance.Name} v{instance.Version}");

            // Load profiles
            var profiles = ServiceSingleton.Packages.ModOrganizer2.GetProfiles();
            DrpDwnLstProfiles.ItemsSource = profiles;
            DrpDwnLstProfiles.SelectedIndex = 0;
        }

        private void ShowLoading() =>
            Dispatcher.UIThread.Post(() => PicBoxLoading.IsVisible = true);

        private void HideLoading() =>
            Dispatcher.UIThread.Post(() => PicBoxLoading.IsVisible = false);

        private async Task<ModObjectList> LoadModStatus()
        {
            ShowLoading();

            try
            {
                ServiceSingleton.Dashboard.Status("Loading mods...");

                var instance = ServiceSingleton.Instances.WorkingInstance;

                var profile = DrpDwnLstProfiles.SelectedItem?.ToString() ?? "";
                _CurrentProfile = profile;

                ModListStatus = await ServiceSingleton.CheckerService.CheckModList(
                    await ServiceSingleton.SoftwareProvider.ModOrganizer2.GetModsMetaData(
                        profile,
                        (s, p) =>
                        {
                            ServiceSingleton.Dashboard.Status($"{s} ({p}%)");
                            ServiceSingleton.Dashboard.Progress(p);
                        }),
                    await ServiceSingleton.Packages.GetModsMetaData(
                        (s, p) =>
                        {
                            ServiceSingleton.Dashboard.Status($"{s} ({p}%)");
                            ServiceSingleton.Dashboard.Progress(p);
                        }),
                    s => ServiceSingleton.Dashboard.Status(s)
                );

                ModListStatus.Profile = profile;

                return ModListStatus;
            }
            finally
            {
                HideLoading();
                ServiceSingleton.Dashboard.NoStatus();
                ServiceSingleton.Dashboard.ProgressCompleted();
            }
        }

        private void LoadGrid(ModObjectList mods)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ModsGrid.ItemsSource = mods.List;
            });
        }

        private async void DrpDwnLstProfiles_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selected = DrpDwnLstProfiles.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selected) || selected == _CurrentProfile)
                    return;

                _CurrentProfile = selected;

                var loaded = await LoadModStatus();
                LoadGrid(loaded);

                UpdateHeaderColor(loaded);
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error loading instance detail", ex.Message);
            }
        }

        private void UpdateHeaderColor(ModObjectList list)
        {
            // Reset
            LblHeader.Text = $"{ServiceSingleton.Instances.WorkingInstance.Name} v{ServiceSingleton.Instances.WorkingInstance.Version}";
            // Default dark background
            LblHeader.Foreground = Brushes.White;

            if (!list.HasMods)
                return;

            if (list.AddedModsCount > 0 || list.RemovedModsCount > 0 || list.IniParsingErrorCount > 0)
            {
                LblHeader.Text += " - Errors Detected";
                LblHeader.Foreground = Brushes.Red;
            }
            else if (list.VersionMismatchCount > 0 || list.InstalledIniMissingCount > 0)
            {
                LblHeader.Text += " - Warnings Detected";
                LblHeader.Foreground = Brushes.Orange;
            }
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Instances.WorkingInstance = null;
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private void BtnPlay_Click(object? sender, RoutedEventArgs e)
        {
            if (!ModOrganizer.IsRunning)
            {
                var instance = ServiceSingleton.Instances.WorkingInstance;

                var mo2 = ModOrganizer.Start(instance.InstallDir);
                BtnPlay.IsEnabled = false;
                BtnPlay.Content = "Running...";

                Task.Run(() =>
                {
                    mo2.WaitForExit();
                    if (mo2.ExitCode == 0)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            BtnPlay.Content = "Play";
                            BtnPlay.IsEnabled = true;
                        });
                    }
                });
            }
            else
            {
                var win = TopLevel.GetTopLevel(this) as Window;
                NolvusMessageBox.Show(win, "Mod Organizer 2", "An instance is already running!", MessageBoxType.Error);
            }
        }

        private async void BtnSettings_Click(object? sender, RoutedEventArgs e)
        {
            var inst = ServiceSingleton.Instances.WorkingInstance;

            switch (inst.Name)
            {
                case Strings.NolvusAscension:
                    await ServiceSingleton.Dashboard.LoadFrameAsync<v5.InstanceSettingsFrame>();
                    break;

                case Strings.NolvusAwakening:
                    // await ServiceSingleton.Dashboard.LoadFrameAsync<v6.InstanceSettingsFrame>();
                    break;
            }
        }

        private async void BtnLoadOrder_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<LoadOrderFrame>();
        }

        private void BtnReport_Click(object? sender, RoutedEventArgs e)
        {
            // For now simple message box until menu is re-implemented
            var win = TopLevel.GetTopLevel(this) as Window;
            NolvusMessageBox.Show(win, "Report", "Report generation menu not yet implemented", MessageBoxType.Info);
        }
    }
}
