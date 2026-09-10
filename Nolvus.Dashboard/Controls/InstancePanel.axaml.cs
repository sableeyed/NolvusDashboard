using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Platform;
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
using Nolvus.Dashboard.Frames.Instance.v5;
using ValveKeyValue;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Interactivity;
using Nolvus.Core.Utils;
using Avalonia.Platform.Storage;
using Nolvus.Dashboard.Frames.Remap.v6;

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
            SetupContextMenu();

            BtnView.Click += BtnView_Click;
            BtnUpdate.Click += BtnUpdate_Click;
            BtnPlay.Click += BtnPlay_Click;
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

            LblInstanceName.Text = _instance.Tag == string.Empty ? _instance.Name : string.Format("{0} - {1}", _instance.Name, _instance.Tag);
            LblVersion.Text = $"{_instance.Performance.Variant} v{_instance.Version}";
            LblDesc.Text = _instance.Description;

            if (await _instance.IsBeta())
                LblVersion.Text += " (Beta)";

            LblStatus.Text = await _instance.GetState();

            if (_instance.Name == Strings.NolvusAscension)
                SetInstanceImage("avares://NolvusDashboard/Assets/Nolvus_V5.png");

            else if (_instance.Name == Strings.NolvusAwakening)
                SetInstanceImage("avares://NolvusDashboard/Assets/Nolvus_V6.png");

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

        private void SetInstanceImage(string Path)
        {
            var uri = new Uri(Path);

            using var asset = AssetLoader.Open(uri);
            using var ms = new MemoryStream();

            asset.CopyTo(ms);
            ms.Position = 0;

            PicInstanceImage.Source = new Bitmap(ms);
        }

        private async void BtnPlay_Click(object? sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;

            if (!Fluorine.IsInstalled)
            {
                await NolvusMessageBox.Show(window, "Fluorine Manager",
                    "Fluorine Manager is not installed. Reinstall or update the instance to install it.", MessageBoxType.Error);
                return;
            }

            if (Fluorine.IsRunning)
            {
                await NolvusMessageBox.Show(window, "Fluorine Manager",
                    "An instance of Fluorine Manager is already running!", MessageBoxType.Error);
                return;
            }

            SetPlayText("Running...");
            BtnPlay.IsEnabled = false;

            try
            {
                // Fluorine opens whatever CurrentInstance names, so point it at this one first.
                ModOrganizer.SelectInstance(_instance.InstallDir);

                var Manager = Fluorine.Start();

                // Fluorine stays up for as long as the user is modding, so the button is held until
                // it exits rather than being restored after a fixed delay.
                _ = Task.Run(() =>
                {
                    try { Manager.WaitForExit(); } catch { }

                    Dispatcher.UIThread.Post(() =>
                    {
                        SetPlayText("Play");
                        BtnPlay.IsEnabled = true;
                    });
                });
            }
            catch (Exception ex)
            {
                SetPlayText("Play");
                BtnPlay.IsEnabled = true;

                ServiceSingleton.Logger.Log($"[FLUORINE] Could not launch : {ex.Message}");

                await NolvusMessageBox.Show(window, "Fluorine Manager",
                    "Unable to start Fluorine Manager : " + ex.Message, MessageBoxType.Error);
            }
        }

        private void BtnUpdate_Click(object? sender, RoutedEventArgs e)
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;
            if (!ModOrganizer.IsRunning)
            {
                ServiceSingleton.Dashboard.LoadFrame<ChangeLogFrame>(new FrameParameters(new FrameParameter { Key = "Instance", Value = _instance }));
            }
            else
            {
                NolvusMessageBox.Show(window, "Mod Organizer 2", "An instance of Mod Organizer 2 is running! Please close it before updating.", MessageBoxType.Error);
            }
        }

        private void BtnView_Click(object? sender, RoutedEventArgs e)
        {
            BtnView.ContextMenu.Open();
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenu();

            // Instance
            var miInstance = new MenuItem { Header = "Instance" };
            miInstance.Click += (_, __) => BrItmMods_Click();
            menu.Items.Add(miInstance);

            // Add Desktop Shortcut
            var miShortcut = new MenuItem { Header = "Add Desktop Shortcut" };
            miShortcut.Click += (_, __) => BrItmShortCut_Click();
            menu.Items.Add(miShortcut);
            
            menu.Items.Add(new Separator());

            // Report to PDF
            var miReport = new MenuItem { Header = "Report to PDF" };
            miReport.Click += (_, __) => BrItmReport_Click();
            menu.Items.Add(miReport);

            menu.Items.Add(new Separator());

            // Keybinds
            var miKeybinds = new MenuItem { Header = "Keybinds" };
            miKeybinds.Click += (_, __) => BrItmKeyBinds_Click();
            menu.Items.Add(miKeybinds);

            menu.Items.Add(new Separator());

            // User Manual
            var miManual = new MenuItem { Header = "User Manual" };
            miManual.Click += (_, __) => BrItmManual_Click();
            menu.Items.Add(miManual);

            menu.Items.Add(new Separator());

            // ENB Manager
            var miEnbManager = new MenuItem { Header = "Enb Manager" };
            miEnbManager.Click += (_, __) => BrItmENBManager_Click();
            menu.Items.Add(miEnbManager);

            menu.Items.Add(new Separator());

            // Delete Instance
            var miDelete = new MenuItem { Header = "Delete Instance" };
            miDelete.Click += (_, __) => BrItmDelete_Click();
            menu.Items.Add(miDelete);

            menu.Items.Add(new Separator());

            var miRemap = new MenuItem { Header = "Remap Instance" };
            miRemap.Click += (_, __) => BrItmRemap_Click();
            menu.Items.Add(miRemap);

            BtnView.ContextMenu = menu;
        }

        private async void BrItmMods_Click()
        {
            ServiceSingleton.Instances.WorkingInstance = _instance;
            await ServiceSingleton.Dashboard.LoadFrameAsync<PackageFrame>(new FrameParameters(new FrameParameter() { Key = "Mode", Value = InstanceMode.View }));
        }

        private async void BrItmReport_Click()
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;
            ServiceSingleton.Instances.WorkingInstance = _instance;
            IDashboard dashboard = ServiceSingleton.Dashboard;

            LockButtons();

            ServiceSingleton.Dashboard.DisableSettings();

            try
            {
                await ServiceSingleton.Packages.Load(
                    await ApiManager.Service.Installer.GetPackage(_instance.Id, _instance.Version),
                    (s, p) =>
                    {
                        dashboard.Status($"{s} ({p}%)");
                        dashboard.Progress(p);
                    });

                var pdf = await ServiceSingleton.Report.GenerateReportToPdf(
                    await ServiceSingleton.CheckerService.CheckModList(
                        await ServiceSingleton.SoftwareProvider.ModOrganizer2.GetModsMetaData(),
                        await ServiceSingleton.Packages.GetModsMetaData(),
                        s => dashboard.Status(s)),
                    LoadImageSharpFromAsset("avares://NolvusDashboard/Assets/background-nolvus.jpg"),
                    (s, p) =>
                    {
                        dashboard.Status($"{s} ({p}%)");
                        dashboard.Progress(p);
                    });

                dashboard.NoStatus();
                dashboard.ProgressCompleted();

                var outputPath = Path.Combine(ServiceSingleton.Folders.ReportDirectory, string.Format("{0}-v{1}.pdf", _instance.Name, _instance.Version));
                if (!Directory.Exists(ServiceSingleton.Folders.ReportDirectory))
                {
                    Directory.CreateDirectory(ServiceSingleton.Folders.ReportDirectory);
                } 
                File.WriteAllBytes(outputPath, pdf);

                NolvusMessageBox.Show(window, "Information", $"PDF report has been generated in {ServiceSingleton.Folders.ReportDirectory}", MessageBoxType.Info);

                Process.Start(ServiceSingleton.Folders.ReportDirectory);
            }
            catch (Exception ex)
            {
                dashboard.NoStatus();
                dashboard.ProgressCompleted();
                ServiceSingleton.Logger.Log(ex.ToString());
                NolvusMessageBox.Show(window, "Error during report generation", ex.Message, MessageBoxType.Error);
            }
            finally
            {
                UnlockButtons();
                ServiceSingleton.Dashboard.EnableSettings();
                ServiceSingleton.Instances.UnloadWorkingIntance();
            }
        }

        private void BrItmKeyBinds_Click()
        {
            switch (_instance.Name)
            {
                case Strings.NolvusAscension:
                    ServiceSingleton.Dashboard.LoadFrame<Nolvus.Dashboard.Frames.Instance.v5.KeysBindingFrame>();
                    break;

                case Strings.NolvusAwakening:
                    ServiceSingleton.Dashboard.LoadFrame<Nolvus.Dashboard.Frames.Instance.v6.KeysBindingFrame>();
                    break;
            }
        }

        private void BrItmDelete_Click()
        {
            ServiceSingleton.Dashboard.LoadFrame<DeleteFrame>(
                new FrameParameters(
                    new FrameParameter { Key = "Instance", Value = _instance },
                    new FrameParameter { Key = "Action", Value = InstanceAction.Delete }));
        }

        
        private void BrItmShortCut_Click()
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;

            try
            {
                if (!Fluorine.IsInstalled)
                {
                    NolvusMessageBox.Show(window, "Desktop Shortcut",
                        "Fluorine Manager is not installed. Reinstall or update the instance to install it.", MessageBoxType.Error);
                    return;
                }

                // --instance opens this instance directly, so the shortcut is not at the mercy of
                // whichever instance Fluorine happened to have selected last. Fluorine is a native
                // binary, so there is no wine involved.
                var ModsDir = Path.Combine(_instance.InstallDir, "MODS");
                var Exec = $"\"{Fluorine.Executable}\" --instance \"{ModsDir}\"";

                var name = _instance.Name;
                var comment = $"Desktop shortcut for your {_instance.Name} instance.";
                var icon = Path.Combine(AppContext.BaseDirectory, "nolvus-ico.jpg");

                CreateDesktopShortcut(name, Exec, comment, Fluorine.InstallDirectory, icon);

                NolvusMessageBox.Show(window, "Desktop Shortcut",
                    $"Your {_instance.Name} shortcut has been added to your desktop. It opens Fluorine Manager on this instance; launch the game from there.",
                    MessageBoxType.Info);
            }
            catch (Exception ex)
            {
                NolvusMessageBox.Show(window, "Error", ex.Message, MessageBoxType.Error);
            }
        }

        private void BrItmManual_Click()
        {
            switch (_instance.Name)
            {
                case Strings.NolvusAscension:
                    Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/asc/appendix/player-guide") { UseShellExecute = true });
                    break;

                case Strings.NolvusAwakening:
                    Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/player-guide") { UseShellExecute = true });
                    break;
            }
        }

        private async void BrItmENBManager_Click()
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;
            if (!ModOrganizer.IsRunning)
            {
                switch (_instance.Name)
                {
                    case Strings.NolvusAscension:
                        NolvusMessageBox.Show(window, "ENB Manager", string.Format("This feature is not available for {0}.", _instance.Name), MessageBoxType.Info);
                        break;
                    case Strings.NolvusAwakening:
                        ServiceSingleton.Instances.WorkingInstance = _instance;
                        await ServiceSingleton.Dashboard.LoadFrameAsync<PackageFrame>(new FrameParameters(new FrameParameter() { Key = "Mode", Value = InstanceMode.ENB }));
                        break;
                }
            }
            else
            {
                NolvusMessageBox.Show(window, "Mod Organizer 2", "An instance of Mod Organizer 2 is running! Close it first.", MessageBoxType.Error);
            }
        }

        private SixLabors.ImageSharp.Image LoadImageSharpFromAsset(string assetPath)
        {
            var uri = new Uri(assetPath);
            using var stream = Avalonia.Platform.AssetLoader.Open(uri);
            return SixLabors.ImageSharp.Image.Load(stream);
        }

        private void CreateDesktopShortcut(string name, string execPath, string comment, string path, string iconPath = "")
        {
            string desktopFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"{name}.desktop");

            string contents =
$@"[Desktop Entry]
Type=Application
Name={name}
Comment={comment}
Exec={execPath}
Path={path}
Icon={iconPath}
Terminal=false
Categories=Game;Utility;
";

            File.WriteAllText(desktopFile, contents);

            Process.Start("chmod", $"+x \"{desktopFile}\"");
        }

        private async void BrItmRemap_Click()
        {
            var owner = TopLevel.GetTopLevel(this) as Window;
            if (!ModOrganizer.IsRunning)
            {
                switch (_instance.Name)
                {
                    case Strings.NolvusAscension:
                        await NolvusMessageBox.Show(owner, "Remap Instance", string.Format("This feature is not available for {0}.", _instance.Name), MessageBoxType.Info);
                        break;
                    
                    case Strings.NolvusAwakening:
                        ServiceSingleton.Instances.WorkingInstance = _instance;
                        await ServiceSingleton.Dashboard.LoadFrameAsync<RemapInstanceFrame>();
                        break;
                }
            }
            else
            {
                await NolvusMessageBox.Show(owner, "Mod Organizer 2", "An instance of Mod Organizer 2 is running! Close it first.", MessageBoxType.Error);
            }
        }
    }
}
