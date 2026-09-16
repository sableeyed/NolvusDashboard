using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

            SetupReportMenu();

            DrpDwnLstProfiles.SelectionChanged += DrpDwnLstProfiles_SelectionChanged;

            // Loading overlay hidden initially
            PicBoxLoading.IsVisible = false;
        }

        protected override async Task OnLoadedAsync()
        {
            var instance = ServiceSingleton.Instances.WorkingInstance;

            LblHeader.Text = string.Format("{0} - {1} v{2}{3}", instance.Name, instance.Performance.Variant, instance.Version, instance.Tag != string.Empty ? string.Format(" - ({0})", instance.Tag) : string.Empty);
            ServiceSingleton.Dashboard.Info($"Instance mods for {instance.Name} v{instance.Version}");

            // Load profiles
            var profiles = await ServiceSingleton.Packages.ModOrganizer2.GetProfilesAsync();
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
                
                ServiceSingleton.Dashboard.DisableSettings();

                _CurrentProfile = selected;

                var loaded = await LoadModStatus();
                LoadGrid(loaded);

                UpdateHeaderColor(loaded);
                LblMO2Profile.IsVisible = true;
                ServiceSingleton.Dashboard.EnableSettings();
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
            ServiceSingleton.Instances.UnloadWorkingIntance();
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private void BtnPlay_Click(object? sender, RoutedEventArgs e)
        {
            // Without this an instance predating the Fluorine switch throws FileNotFoundException
            // out of an async void handler, so the user sees nothing at all. The offer to install
            // lives on the instance list's play button.
            if (!Fluorine.IsInstalled)
            {
                NolvusMessageBox.Show(TopLevel.GetTopLevel(this) as Window, "Fluorine Manager",
                    "Fluorine Manager is not installed. Go back to the instance list and press Play there to install it.",
                    MessageBoxType.Error);
                return;
            }

            if (!ModOrganizer.IsRunning)
            {
                var instance = ServiceSingleton.Instances.WorkingInstance;

                ModOrganizer.EnsureInstanceIni(instance.InstallDir);

                ModOrganizer.SelectInstance(instance.InstallDir);

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
                    await ServiceSingleton.Dashboard.LoadFrameAsync<v6.InstanceSettingsFrame>();
                    break;
            }
        }

        private async void BtnLoadOrder_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<LoadOrderFrame>();
        }

        private void SetupReportMenu()
        {
            var Menu = new ContextMenu();

            var MiClipboard = new MenuItem { Header = "Copy to clipboard" };
            MiClipboard.Click += (_, __) => BrItmClipboardReport_Click();
            Menu.Items.Add(MiClipboard);

            var MiPdf = new MenuItem { Header = "Generate PDF" };
            MiPdf.Click += (_, __) => BrItmPDFReport_Click();
            Menu.Items.Add(MiPdf);

            BtnReport.ContextMenu = Menu;
        }

        private void BtnReport_Click(object? sender, RoutedEventArgs e)
        {
            BtnReport.ContextMenu.Open();
        }

        private async void BrItmClipboardReport_Click()
        {
            var Window_ = TopLevel.GetTopLevel(this) as Window;

            ShowLoading();

            try
            {
                var Report = await ServiceSingleton.Report.GenerateReportToClipBoard(ModListStatus, (s, p) =>
                {
                    ServiceSingleton.Dashboard.Status($"{s} ({p}%)");
                    ServiceSingleton.Dashboard.Progress(p);
                });

                var Clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

                if (Clipboard == null)
                    throw new Exception("No clipboard is available");

                await Clipboard.SetTextAsync(Report);

                ReportDone();

                await NolvusMessageBox.Show(Window_, "Information", "Configuration has been copied to the clipboard", MessageBoxType.Info);
            }
            catch (Exception ex)
            {
                ReportDone();

                ServiceSingleton.Logger.Log(ex.ToString());

                await NolvusMessageBox.Show(Window_, "Error during report generation", ex.Message, MessageBoxType.Error);
            }
        }

        private async void BrItmPDFReport_Click()
        {
            var Window_ = TopLevel.GetTopLevel(this) as Window;

            ShowLoading();

            try
            {
                var Instance = ServiceSingleton.Instances.WorkingInstance;

                var Pdf = await ServiceSingleton.Report.GenerateReportToPdf(
                    ModListStatus,
                    LoadImageSharpFromAsset("avares://NolvusDashboard/Assets/background-nolvus.jpg"),
                    (s, p) =>
                    {
                        ServiceSingleton.Dashboard.Status($"{s} ({p}%)");
                        ServiceSingleton.Dashboard.Progress(p);
                    });

                Directory.CreateDirectory(ServiceSingleton.Folders.ReportDirectory);

                File.WriteAllBytes(
                    Path.Combine(ServiceSingleton.Folders.ReportDirectory, string.Format("{0}-v{1}.pdf", Instance.Name, Instance.Version)),
                    Pdf);

                ReportDone();

                await NolvusMessageBox.Show(Window_, "Information", $"PDF report has been generated in {ServiceSingleton.Folders.ReportDirectory}", MessageBoxType.Info);

                OpenReportDirectory();
            }
            catch (Exception ex)
            {
                ReportDone();

                ServiceSingleton.Logger.Log(ex.ToString());

                await NolvusMessageBox.Show(Window_, "Error during report generation", ex.Message, MessageBoxType.Error);
            }
        }

        private void ReportDone()
        {
            HideLoading();
            ServiceSingleton.Dashboard.NoStatus();
            ServiceSingleton.Dashboard.ProgressCompleted();
        }

        private void OpenReportDirectory()
        {
            // UseShellExecute, or this tries to run the directory as a program.
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ServiceSingleton.Folders.ReportDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"Unable to open the report directory : {ex.Message}");
            }
        }

        private SixLabors.ImageSharp.Image LoadImageSharpFromAsset(string AssetPath)
        {
            using var Stream = Avalonia.Platform.AssetLoader.Open(new Uri(AssetPath));

            return SixLabors.ImageSharp.Image.Load(Stream);
        }
    }
}
