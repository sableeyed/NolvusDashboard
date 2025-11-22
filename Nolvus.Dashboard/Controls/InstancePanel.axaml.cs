using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Enums;
using Nolvus.Core.Misc;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Frames;
using Nolvus.Dashboard.Frames.Installer;
using Nolvus.Dashboard.Frames.Instance;
using Nolvus.Package.Mods;
using Vcc.Nolvus.Api.Installer.Services;

namespace Nolvus.Dashboard.Controls
{
    public partial class InstancePanel : UserControl
    {
        private INolvusInstance _instance;
        private InstancesPanel _parent;

        public InstancePanel(InstancesPanel parent)
        {
            InitializeComponent();
            _parent = parent;
            //SetupContextMenu();
        }

        private void LockButtons()
        {
            BtnPlay.IsEnabled = false;
            BtnUpdate.IsEnabled = false;
            BtnView.IsEnabled = false;

            (_parent.ContainerFrame as InstancesFrame)?.LockButtons();
        }

        private void UnlockButtons()
        {
            BtnPlay.IsEnabled = true;
            BtnUpdate.IsEnabled = true;
            BtnView.IsEnabled = true;

            (_parent.ContainerFrame as InstancesFrame)?.UnLockButtons();
        }

        private void SetPlayText(string text)
        {
            Dispatcher.UIThread.Post(() =>
            {
                BtnPlay.Content = text;
                BtnPlay.IsEnabled = true;
            });
        }

        public async void LoadInstance(INolvusInstance instance)
        {
            _instance = instance;

            LblInstanceName.Text = _instance.Name;
            LblVersion.Text = $"{_instance.Performance.Variant} v{_instance.Version}";
            LblDesc.Text = _instance.Description;

            if (await _instance.IsBeta())
                LblVersion.Text += " (Beta)";

            LblStatus.Text = await _instance.GetState();

            // if (_instance.Name == Strings.NolvusAscension)
            //     SetInstanceImage(Properties.Resources.Nolvus_V5);

            // else if (_instance.Name == Strings.NolvusAwakening)
            //     SetInstanceImage(Properties.Resources.Nolvus_V6);

            LblImageLoading.IsVisible = false;

            if (LblStatus.Text == "Installed")
            {
                LblStatus.Foreground = Avalonia.Media.Brushes.Orange;
            }
            else if (LblStatus.Text.Contains("New version available"))
            {
                LblStatus.Foreground = Avalonia.Media.Brushes.Orange;
                BtnUpdate.IsVisible = true;
            }
        }

        // private void SetInstanceImage(System.Drawing.Image img)
        // {
        //     using (var ms = new MemoryStream())
        //     {
        //         img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //         ms.Position = 0;
        //         PicInstanceImage.Source = new Bitmap(ms);
        //     }
        // }

        private void BtnPlay_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;
            if (!ModOrganizer.IsRunning)
            {
                Process mo2 = ModOrganizer.Start(_instance.InstallDir);

                BtnPlay.Content = "Running...";
                BtnPlay.IsEnabled = false;

                Task.Run(() =>
                {
                    mo2.WaitForExit();
                    if (mo2.ExitCode == 0)
                        SetPlayText("Play");
                });
            }
            else
            {
                NolvusMessageBox.Show(window, "Mod Organizer 2", "An instance of Mod Organizer 2 is already running!", MessageBoxType.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;
            if (!ModOrganizer.IsRunning)
            {
                //ServiceSingleton.Dashboard.LoadFrame<ChangeLogFrame>(new FrameParameters(new FrameParameter { Key = "Instance", Value = _instance }));
            }
            else
            {
                NolvusMessageBox.Show(window, "Mod Organizer 2", "An instance of Mod Organizer 2 is running! Please close it before updating.", MessageBoxType.Error);
            }
        }

        private void BtnView_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            BtnView.ContextMenu.Open();
        }

        private void SetupContextMenu()
        {
            // BtnView.ContextMenu = new ContextMenu
            // {
            //     Items =
            //     {
            //         new MenuItem { Header = "Instance", Command = ReactiveCommand.Create(BrItmMods_Click) },
            //         new MenuItem { Header = "Add Desktop Shortcut", Command = ReactiveCommand.Create(BrItmShortCut_Click) },
            //         new Separator(),
            //         new MenuItem { Header = "Report to PDF", Command = ReactiveCommand.Create(BrItmReport_Click) },
            //         new Separator(),
            //         new MenuItem { Header = "Keybinds", Command = ReactiveCommand.Create(BrItmKeyBinds_Click) },
            //         new Separator(),
            //         new MenuItem { Header = "User Manual", Command = ReactiveCommand.Create(BrItmManual_Click) },
            //         new Separator(),
            //         new MenuItem { Header = "Delete Instance", Command = ReactiveCommand.Create(BrItmDelete_Click) }
            //     }
            // };
        }

        private async void BrItmMods_Click()
        {
            ServiceSingleton.Instances.WorkingInstance = _instance;
            await ServiceSingleton.Dashboard.LoadFrameAsync<PackageFrame>();
        }

        // private async void BrItmReport_Click()
        // {
        //     var window = TopLevel.GetTopLevel(this) as DashboardWindow;
        //     ServiceSingleton.Instances.WorkingInstance = _instance;
        //     IDashboard dashboard = ServiceSingleton.Dashboard;

        //     LockButtons();

        //     try
        //     {
        //         await ServiceSingleton.Packages.Load(
        //             await ApiManager.Service.Installer.GetPackage(_instance.Id, _instance.Version),
        //             (s, p) =>
        //             {
        //                 dashboard.Status($"{s} ({p}%)");
        //                 dashboard.Progress(p);
        //             });

        //         await ServiceSingleton.Report.GenerateReportToPdf(
        //             await ServiceSingleton.CheckerService.CheckModList(
        //                 await ServiceSingleton.SoftwareProvider.ModOrganizer2.GetModsMetaData(),
        //                 await ServiceSingleton.Packages.GetModsMetaData(),
        //                 s => dashboard.Status(s)),
        //             Properties.Resources.background_nolvus,
        //             (s, p) =>
        //             {
        //                 dashboard.Status($"{s} ({p}%)");
        //                 dashboard.Progress(p);
        //             });

        //         dashboard.NoStatus();
        //         dashboard.ProgressCompleted();

        //         NolvusMessageBox.Show(window, "Information", $"PDF report has been generated in {ServiceSingleton.Folders.ReportDirectory}", MessageBoxType.Info);

        //         Process.Start(ServiceSingleton.Folders.ReportDirectory);
        //     }
        //     catch (Exception ex)
        //     {
        //         dashboard.NoStatus();
        //         dashboard.ProgressCompleted();
        //         NolvusMessageBox.Show(window, "Error during report generation", ex.Message, MessageBoxType.Error);
        //     }
        //     finally
        //     {
        //         UnlockButtons();
        //         ServiceSingleton.Instances.UnloadWorkingIntance();
        //     }
        // }

        // private void BrItmKeyBinds_Click()
        // {
        //     switch (_instance.Name)
        //     {
        //         case Strings.NolvusAscension:
        //             ServiceSingleton.Dashboard.LoadFrame<Nolvus.Dashboard.Frames.Instance.v5.KeysBindingFrame>();
        //             break;

        //         case Strings.NolvusAwakening:
        //             ServiceSingleton.Dashboard.LoadFrame<Nolvus.Dashboard.Frames.Instance.v6.KeysBindingFrame>();
        //             break;
        //     }
        // }

        private void BrItmDelete_Click()
        {
            ServiceSingleton.Dashboard.LoadFrame<DeleteFrame>(
                new FrameParameters(
                    new FrameParameter { Key = "Instance", Value = _instance },
                    new FrameParameter { Key = "Action", Value = InstanceAction.Delete }));
        }

        // private void BrItmShortCut_Click()
        // {
        //     var window = TopLevel.GetTopLevel(this) as DashboardWindow;
        //     try
        //     {
        //         string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        //         string shortcutPath = Path.Combine(desktop, $"{_instance.Name}.lnk");

        //         ShortcutCreator.CreateWindowsShortcut(shortcutPath, Path.Combine(_instance.InstallDir, "MO2", "ModOrganizer.exe"), $"Desktop shortcut for your {_instance.Name} instance.");

        //         NolvusMessageBox.Show(window, "Desktop Shortcut", $"Your {_instance.Name} shortcut has been added to your desktop.", MessageBoxType.Info);
        //     }
        //     catch (Exception ex)
        //     {
        //         NolvusMessageBox.Show(window, "Error", ex.Message, MessageBoxType.Error);
        //     }
        // }

        private void BrItmManual_Click()
        {
            switch (_instance.Name)
            {
                case Strings.NolvusAscension:
                    Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/asc/appendix/playerguide") { UseShellExecute = true });
                    break;

                case Strings.NolvusAwakening:
                    Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/playerguide") { UseShellExecute = true });
                    break;
            }
        }
    }
}
