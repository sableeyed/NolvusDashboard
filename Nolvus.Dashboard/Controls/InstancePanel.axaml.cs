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
using Nolvus.Dashboard.Services;
using ValveKeyValue;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Interactivity;
using Nolvus.Core.Utils;
using Avalonia.Platform.Storage;
using Nolvus.Dashboard.Services.Wine;
using Nolvus.Dashboard.Services.Proton;
using Nolvus.Dashboard.Frames.Manager.Proton;

namespace Nolvus.Dashboard.Controls
{
    public partial class InstancePanel : UserControl
    {
        private INolvusInstance _instance;
        private InstancesPanel _parent;
        private const string LauncherHash = "03559ba20b8cae267508c4a4849bc2a6";
        private const string NolvusHash = "d28d77bf715d577f21145bb792e70af8";

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

            if (ModOrganizer.IsRunning)
            {
                NolvusMessageBox.Show(window, "Mod Organizer 2", "An instance of Mod Organizer 2 is already running!", MessageBoxType.Error);
                return;
            }

            SetPlayText("Running...");
            BtnPlay.IsEnabled = false;

            var psi = new ProcessStartInfo
            {
                FileName = ExecutableResolver.RequireExecutable("steam"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            psi.ArgumentList.Add("steam://rungameid/489830");

            try
            {
                using var proc = Process.Start(psi);
                await Task.Delay(1500);
                SetPlayText("Play");
                BtnPlay.IsEnabled = true;
            }
            catch
            {
                SetPlayText("Play");
                BtnPlay.IsEnabled = true;
                throw;
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

            //Nolvus Launcher
            var miRedirector = new MenuItem { Header = "Setup Steam Redirector" };
            miRedirector.Click += (_, __) => BrItmRedirector_Click();
            menu.Items.Add(miRedirector);

            //MO2 Prefix (optional)
            var miMO2Prefix = new MenuItem { Header = "Create MO2 Prefix" };
            miMO2Prefix.Click += async (_, __) => await BrMO2Prefix_Click();
            menu.Items.Add(miMO2Prefix);

            //Skyrim Proton Prefix
            var miPostInstall = new MenuItem { Header = "Proton Manager" };
            miPostInstall.Click += async (_, __) => await BrItemPostInstall_Click();
            menu.Items.Add(miPostInstall);

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
                string winePrefix = WinePrefix.PrefixPath;
                string mo2Path = Path.Combine(_instance.InstallDir, "MO2", "ModOrganizer.exe");
                string exec = $"env WINEPREFIX=\"{winePrefix}\" wine \"{mo2Path}\"";
                string path = Path.Combine(_instance.InstallDir, "MO2");
                var name = _instance.Name;
                var comment = $"Desktop shortcut for your {_instance.Name} instance.";
                var icon = Path.Combine(AppContext.BaseDirectory, "nolvus-ico.jpg");

                CreateDesktopShortcut(name, exec, comment, path, icon);

                NolvusMessageBox.Show(window, "Desktop Shortcut", $"Your {_instance.Name} shortcut has been added to your desktop. " +
                                "This is only for using MO2 without Steam. You will not be able to launch the game this way", MessageBoxType.Info);
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
                    Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/asc/appendix/playerguide") { UseShellExecute = true });
                    break;

                case Strings.NolvusAwakening:
                    Process.Start(new ProcessStartInfo("https://www.nolvus.net/guide/awake/appendix/playerguide") { UseShellExecute = true });
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

        private async Task BrItmRedirector_Click()
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); //get hpme folder
            string vdfPath = Path.Combine(home, ".steam", "steam", "config", "libraryfolders.vdf"); //get path to vdf
            string? libraryPath = GetSteamLibraryForApp(vdfPath); //get path to the steam library from vdf
            string? skyrimPath = Path.Combine(libraryPath, "steamapps", "common", "Skyrim Special Edition"); //combine paths to get skyrim path
            string mo2Path = Path.Combine(_instance.InstallDir, "MO2", "ModOrganizer.exe");

            if (skyrimPath == null)
            {
                ServiceSingleton.Logger.Log("Skyrim Path not found - manual intervention required");
                return;
            }

            string? launcherPath = Path.Combine(skyrimPath, "SkyrimSELauncher.exe");
            if (!File.Exists(launcherPath))
            {
                NolvusMessageBox.Show(window, "Error", "SkyrimSE.exe not found. If you believe this is a bug, install the launcher manually.", MessageBoxType.Error);
                return;
            }

            //Check if the launcher is already installed
            string? md5 = GetFileMd5(launcherPath);
            if(md5 != null && md5.ToLowerInvariant() == NolvusHash)
            {
                NolvusMessageBox.Show(window, "Error", "Redirector already exists - Update your instancepath.txt manually", MessageBoxType.Error);
                return;
            }

            //setup launcher if not
            if(md5 != null && md5.ToLowerInvariant() == LauncherHash)
            {
                string temp = launcherPath + ".new";

                ExtractLauncher(temp);

                File.Move(launcherPath, launcherPath + ".bak");
                File.Move(temp, launcherPath);
                var instancePath = Path.Combine(skyrimPath, "instancepath.txt");
                File.WriteAllText(instancePath, mo2Path);

                await NolvusMessageBox.Show(window, "Success", "Skyrim Redirector installed", MessageBoxType.Info);
            }
        }

        private async Task BrItemPostInstall_Click()
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;

            bool? result = await NolvusMessageBox.ShowConfirmation(window, "Skyrim Prefix", "In order to play Nolvus this step is mandatory. Please ensure winetricks/protontricks is up to date otherwise this may fail silently. Winetricks should be self updated with \"sudo winetricks --self-update\". If you encounter issues, please refer to the wiki on how to do this manually.");

            if (result != true)
                return;
            
            await ServiceSingleton.Dashboard.LoadFrameAsync<ProtonManagerFrame>();
        }


        private async Task BrMO2Prefix_Click()
        {
            var window = TopLevel.GetTopLevel(this) as DashboardWindow;
            bool? result = await NolvusMessageBox.ShowConfirmation(window, "ModOrganizer", "This is only useful if you want to use MO2 without needing to launch steam. You CANNOT play the game through MO2 with this method. Do you want to continue?");
            if (result == true)
            {
                var winePath = ExecutableResolver.FindExecutable("wine");
                if (winePath == null)
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel == null) 
                    {
                        await ServiceSingleton.Dashboard.Error("MO2", "An error ocurred when trying to open system file dialog.");
                        return;
                    }

                    var binary = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Select Wine executable",
                        AllowMultiple = false
                    });

                    if (binary == null || binary.Count == 0)
                    {
                        await ServiceSingleton.Dashboard.Error("MO2", "Wine binary was not selected. Please provide a valid wine binary.");
                        return;
                    }

                    winePath = binary[0].Path.LocalPath;
                }

                WineRunner.WinePath = winePath;

                await WinePrefix.InitializeAsync((_, __) => { });
            }
        }

        private static void ExtractLauncher(string output)
        {
            var asm = typeof(DashboardApp).Assembly;

            string resourceName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("NolvusLauncher.exe"));

            if (resourceName == null)
                throw new Exception("Launcher not found");

            using Stream? resStream = asm.GetManifestResourceStream(resourceName);

            if (resStream == null)
                throw new Exception("Failed to load launcher");

            using FileStream fs = File.Create(output);
            resStream.CopyTo(fs);
        }

        private static string? GetFileMd5(string path)
        {
            if (!File.Exists(path))
                return null;

            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);

            byte[] hash = md5.ComputeHash(stream);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                sb.Append(b.ToString("x2"));
            
            return sb.ToString();
        }

        public static string? GetSteamLibraryForApp(string vdfPath, string appId = "489830")
        {
            if (!File.Exists(vdfPath))
                return null;

            string[] lines = File.ReadAllLines(vdfPath);

            string? currentPath = null;
            bool insideLibrary = false;
            bool insideApps = false;

            foreach (var raw in lines)
            {
                string line = raw.Trim();

                // Start of a library entry: "0", "1", "2", ...
                if (line.StartsWith("\"") && line.EndsWith("\"") && line.Length <= 4)
                {
                    // New library block starting
                    insideLibrary = true;
                    insideApps = false;
                    currentPath = null;
                    continue;
                }

                if (!insideLibrary)
                    continue;

                // Detect path
                if (line.StartsWith("\"path\""))
                {
                    // Extract value between quotes: "path"   "xxxxx"
                    int idx = line.IndexOf('"', 7);
                    if (idx > 0)
                    {
                        int end = line.IndexOf('"', idx + 1);
                        if (end > idx)
                            currentPath = line.Substring(idx + 1, end - (idx + 1));
                    }
                    continue;
                }

                // Detect entering apps block
                if (line.StartsWith("\"apps\""))
                {
                    insideApps = true;
                    continue;
                }

                // Inside apps block, check for appId
                if (insideApps && line.StartsWith($"\"{appId}\""))
                {
                    // Found Skyrim SE inside this library
                    return currentPath;
                }

                // Leaving blocks
                if (line == "}")
                {
                    if (insideApps)
                    {
                        insideApps = false;
                    }
                    else if (insideLibrary)
                    {
                        insideLibrary = false;
                        currentPath = null;
                    }
                }
            }

            return null;
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

    }
}
