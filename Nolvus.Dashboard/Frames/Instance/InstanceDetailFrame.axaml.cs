using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
        public static readonly FuncValueConverter<ModObjectStatus, IBrush> StatusBrush = new(Status =>
        {
            if (Status == ModObjectStatus.OK)
            {
                return Brushes.LimeGreen;
            }
            else if (Status == ModObjectStatus.VersionMisMatch || Status == ModObjectStatus.InstalledIniMissing)
            {
                return Brushes.Orange;
            }
            else
            {
                return Brushes.Red;
            }
        });

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
            BtnExpand.Click += BtnExpand_Click;
            BtnCollapse.Click += BtnCollapse_Click;

            ModsGrid.DoubleTapped += ModsGrid_DoubleTapped;

            DrpDwnLstProfiles.SelectionChanged += DrpDwnLstProfiles_SelectionChanged;

            // Loading overlay hidden initially
            PicBoxLoading.IsVisible = false;
        }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                var instance = ServiceSingleton.Instances.WorkingInstance;

                LblHeader.Text = instance.FullName;
                ServiceSingleton.Dashboard.Info(string.Format("Instance mods for {0}", instance.FullName));

                // Load profiles
                var profiles = await ServiceSingleton.Packages.ModOrganizer2.GetProfilesAsync();
                DrpDwnLstProfiles.ItemsSource = profiles;
                DrpDwnLstProfiles.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                await ServiceSingleton.Dashboard.Error("Error during instance detail loading", ex.Message);
            }
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
                var ModsView = new DataGridCollectionView(mods.List);
                ModsView.GroupDescriptions.Add(new DataGridPathGroupDescription("Category"));

                ModsGrid.ItemsSource = ModsView;

                // The grid would otherwise select the first mod, upstream's starts with nothing.
                ModsView.MoveCurrentToPosition(-1);
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
            LblHeader.Text = ServiceSingleton.Instances.WorkingInstance.FullName;
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

        private void BtnExpand_Click(object? sender, RoutedEventArgs e)
        {
            if (ModsGrid.ItemsSource is DataGridCollectionView ModsView)
            {
                foreach (DataGridCollectionViewGroup Group in ModsView.Groups)
                {
                    ModsGrid.ExpandRowGroup(Group, true);
                }
            }
        }

        private void BtnCollapse_Click(object? sender, RoutedEventArgs e)
        {
            if (ModsGrid.ItemsSource is DataGridCollectionView ModsView && ModsView.Groups.Count > 0)
            {
                // Collapsing a group whose header is scrolled off but whose rows are on screen
                // leaves the grid's displayed rows stale, and CollapseRowGroup then throws on the
                // next header. From the top every header is on screen or below it.
                ModsGrid.ScrollIntoView(ModsView.Groups[0], null);
                ModsGrid.UpdateLayout();

                foreach (DataGridCollectionViewGroup Group in ModsView.Groups)
                {
                    ModsGrid.CollapseRowGroup(Group, true);
                }
            }
        }

        private async void ModsGrid_DoubleTapped(object? sender, TappedEventArgs e)
        {
            // Unlike upstream's CellDoubleClick this also fires on the column and group headers,
            // which have no mod row above them.
            var Mod = (e.Source as Visual)?.FindAncestorOfType<DataGridRow>(true)?.DataContext as ModObject;
            var Owner = TopLevel.GetTopLevel(this) as Window;

            if (Mod == null || Owner == null)
                return;

            var Info = string.Format("Mod : {0}", Mod.Name, Mod.Version);

            Info += Environment.NewLine;
            Info += string.Format("Version : {0}", Mod.Version);
            Info += Environment.NewLine;
            Info += string.Format("Status : {0}", Mod.StatusText);

            await NolvusMessageBox.Show(Owner, "Mod information", Info, MessageBoxType.Info);
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

                ModOrganizer.EnsureInstanceIni(instance.SettingsManager.InstallDir);

                ModOrganizer.SelectInstance(instance.SettingsManager.InstallDir);

                Fluorine.InstallPlugins(instance.SettingsManager.InstallDir);

                var mo2 = ModOrganizer.Start(instance.SettingsManager.InstallDir);
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
            await ServiceSingleton.Dashboard.LoadFrameAsync<InstanceSettingsFrame>();
        }

        private async void BtnLoadOrder_Click(object? sender, RoutedEventArgs e)
        {
            await ServiceSingleton.Dashboard.LoadFrameAsync<LoadOrderFrame>();
        }

        private async void BtnReport_Click(object? sender, RoutedEventArgs e)
        {
            var Window_ = TopLevel.GetTopLevel(this) as Window;

            ShowLoading();

            try
            {
                var Instance = ServiceSingleton.Instances.WorkingInstance;

                var Pdf = await ServiceSingleton.Report.GenerateReportToPdf(
                    ModListStatus,
                    LoadTextFromAsset("avares://NolvusDashboard/Assets/background-nolvus.svg"),
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

        private string LoadTextFromAsset(string AssetPath)
        {
            using var Stream = Avalonia.Platform.AssetLoader.Open(new Uri(AssetPath));
            using var Reader = new StreamReader(Stream);

            return Reader.ReadToEnd();
        }
    }
}
