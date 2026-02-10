using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nolvus.Core.Frames;
using Nolvus.Dashboard.Frames.Instance;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Dashboard.Services.Proton;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Chrome;

namespace Nolvus.Dashboard.Frames.Manager.Proton
{
    public partial class ProtonManagerFrame : DashboardFrame
    {
        private INolvusInstance Instance;
        public ProtonManagerFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
            ServiceSingleton.Dashboard.Info("Proton Prefix Configuration");
            ServiceSingleton.Dashboard.Title("Nolvus Dashboard - Post Installation");
        }

        protected override async Task OnLoadedAsync()
        {
            Instance = ServiceSingleton.Instances.WorkingInstance;
            await DetectProtonRunners();
            await DetectPrefix();
        }

        private async Task DetectProtonRunners()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var ProtonDir = Path.Combine(home, ".local", "share", "Steam", "compatibilitytools.d");
            var FlatpakDir = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam", "compatibilitytools.d");
            
            if (!Directory.Exists(ProtonDir) && (!Directory.Exists(FlatpakDir)))
            {
                await ServiceSingleton.Dashboard.Error("Error", "No Proton folders found, have you launched Steam?");
                return;
            }

            string[] ProtonPaths = Array.Empty<string>();
            string[] FlatpakPaths = Array.Empty<string>();

            if (Directory.Exists(ProtonDir))
                ProtonPaths = Directory.GetDirectories(ProtonDir);
            
            if (Directory.Exists(FlatpakDir))
                FlatpakPaths = Directory.GetDirectories(FlatpakDir);
            
            var Runners = ProtonPaths.Select(p => new DirectoryInfo(p).Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            var FlatpakRunners = FlatpakPaths.Select(p => new DirectoryInfo(p).Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

            if (Runners.Contains("LegacyRuntime"))
                Runners.Remove("LegacyRuntime");

            if (FlatpakRunners.Contains("LegacyRuntime"))
                FlatpakRunners.Remove("LegacyRuntime");

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Runners.Count > 0)
                {
                    DrpDwnLstProtonRunner.ItemsSource = Runners;
                    DrpDwnLstProtonRunner.SelectedIndex = 0;
                }
                else if (FlatpakRunners.Count > 0)
                {                
                    DrpDwnLstProtonRunner.ItemsSource = FlatpakRunners;
                    DrpDwnLstProtonRunner.SelectedIndex = 0;
                }
                else
                {
                    ProtonPathBox.IsEnabled = true;
                    ProtonPathButton.IsEnabled = true;
                }
            });
        }

        private async Task DetectPrefix()
        {
            string AppId = "489830";

            var PrefixPath = await Protontricks.GetPrefixPathAsync(AppId);

            if (!string.IsNullOrWhiteSpace(PrefixPath))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PrefixDirectoryBox.IsEnabled = true;
                    PrefixDirectoryBox.Text = PrefixPath;
                });
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    PrefixDirectoryBox.IsEnabled = true;
                    PrefixDirectoryButton.IsEnabled = true;
                    PrefixDirectoryBox.Text = "Prefix not found! Please browse manually. (NOT IMPLEMENTED)";
                });
            }
        }

        private void BtnBack_Click(object? sender, RoutedEventArgs e)
        {
            ServiceSingleton.Instances.UnloadWorkingIntance();
            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
        }

        private async void BtnInstall_Click(object? sender, RoutedEventArgs e)
        {
            const string appId = "489830";
            
            //Unable to detect Protons
            if (ProtonPathBox.IsEnabled)
            {
                var ProtonPath = ProtonPathBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(ProtonPath))
                {
                    await ServiceSingleton.Dashboard.Error("Error", "Please browse for a Proton folder, as Auto Detection failed.");
                    return;
                }

                if (!Directory.Exists(ProtonPath))
                {
                    await ServiceSingleton.Dashboard.Error("Error", "Selected Proton folder does not exist. Please try manually through terminal");
                    return;
                }

                //Ensure it's a real Proton install
                var ProtonScript = Path.Combine(ProtonPath, "proton");
                if (!File.Exists(ProtonScript))
                {
                    await ServiceSingleton.Dashboard.Error("Error", "The selected folder does not contain a proton binary!");
                    return;
                }

                await Protontricks.ConfigureAsync(appId, instanceInstallDir: Instance.InstallDir, protonVersion: null, protonPath: ProtonPath);
                return;
            }
            
            var Runner = DrpDwnLstProtonRunner.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(Runner))
            {
                await ServiceSingleton.Dashboard.Error("Error", "Error getting Proton Runner");
                return;
            }

            await Protontricks.ConfigureAsync(appId, instanceInstallDir: Instance.InstallDir, protonVersion: Runner, protonPath: null);
        }

        private async void PrefixPath_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider is null)
            {
                await ServiceSingleton.Dashboard.Error("Error", "Unable to start up Storage Provider. You will need to configure manually using terminal.");
                return;
            }

            var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Proton prefix folder (pfx)",
                AllowMultiple = false
            });

            var folder = result.Count > 0 ? result[0] : null;
            if (folder is null)
                return;
            
            var path = folder.Path.LocalPath;

            if (!path.EndsWith($"{Path.DirectorySeparatorChar}pfx", StringComparison.OrdinalIgnoreCase) && !path.EndsWith("/pfx", StringComparison.OrdinalIgnoreCase))
            {
                await ServiceSingleton.Dashboard.Error("Invalid selection", "Please select the 'pfx' folder (…/steamapps/compatdata/489830/pfx).");
                return;
            }

            PrefixDirectoryBox.Text = path;
        }

        private async void ProtonPath_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.StorageProvider is null)
                return;

            var results = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Proton folder (root)",
                AllowMultiple = false
            });

            var folder = results.Count > 0 ? results[0] : null;
            if (folder is null)
                return;

            var path = folder.Path.LocalPath;

            var protonScript = Path.Combine(path, "proton");
            if (!File.Exists(protonScript))
            {
                await ServiceSingleton.Dashboard.Error("Invalid selection", "That folder doesn't look like a Proton install (missing 'proton' script). Please select the Proton root folder.");
                return;
            }

            ProtonPathBox.Text = path;
        }
    }
}