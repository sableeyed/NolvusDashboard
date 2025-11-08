using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Nolvus.Api.Installer.Services;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Events;
using Nolvus.Services.Files;
using Nolvus.Services.Globals;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Nolvus.Updater
{
    public partial class MainWindow : Window
    {
        private bool _error;
        private readonly bool _closeApp;

        private void Log(string msg)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "nolvus-updater.log"),
                    $"[{DateTime.Now:HH:mm:ss}] {msg}\n"
                );
            }
            catch { /* ignore */ }
        }


        public MainWindow()
        {
            InitializeComponent();

            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#363636"));

            var args = Environment.GetCommandLineArgs();
            _closeApp = args.Length > 1 && args[1] == "1";

            ServiceSingleton.RegisterService<IGlobalsService>(new GlobalsService());

            _ = RunAsync();
        }

        #region UI helpers

        private static Task UI(Action a) => Dispatcher.UIThread.InvokeAsync(a).GetTask();

        private Task SetInfo(string value) => UI(() => LblInfo.Text = value);
        private Task SetProgress(int value) => UI(() => ProgressBar.Value = value);
        private Task ShowButton(bool value) => UI(() => BtnClose.IsVisible = value);

        private Task SetError(string value)
        {
            return UI(() =>
            {
                LblInfo.Text = string.Empty;
                ProgressBar.Value = 0;
                LblError.IsVisible = true;
                LblError.Text = value;
                _error = true;
            });
        }

        #endregion

        #region Paths & checks

        private static string BaseFolder =>
            Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        private static string DashboardName => "NolvusDashboard"; // no .exe on Linux
        private static string DashboardPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DashboardName);

        private static bool IsRoot(string path)
        {
            try { return new DirectoryInfo(path).Parent == null; } catch { return false; }
        }

        private static bool IsInside(string child, string parent)
        {
            if (string.IsNullOrWhiteSpace(parent)) return false;
            var fullChild = Path.GetFullPath(child) + Path.DirectorySeparatorChar;
            var fullParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return fullChild.StartsWith(fullParent, StringComparison.OrdinalIgnoreCase);
        }

        private static string DesktopFolder => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static string DocumentsFolder => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static string DownloadsFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        #endregion

        private async Task RunAsync()
        {
            await SetProgress(0);
            await SetInfo("Initializing...");

            // location checks (mirror original intent)
            if (IsInside(BaseFolder, DownloadsFolder))
            {
                await SetError("Nolvus Dashboard cannot be installed in your Downloads folder. Please move the updater to another folder.");
                await ShowButton(true);
                return;
            }
            if (IsInside(BaseFolder, DesktopFolder))
            {
                await SetError("Nolvus Dashboard cannot be installed on your Desktop. Please move the updater to another folder.");
                await ShowButton(true);
                return;
            }
            if (IsInside(BaseFolder, DocumentsFolder))
            {
                await SetError("Nolvus Dashboard cannot be installed in your Documents folder. Please move the updater to another folder.");
                await ShowButton(true);
                return;
            }
            if (IsRoot(BaseFolder))
            {
                await SetError("Nolvus Dashboard cannot be installed on a root drive. Please move the updater to another folder.");
                await ShowButton(true);
                return;
            }

            try
            {
                await SetProgress(10);
                await InitApi();
                await SetProgress(50);

                await SetInfo("Checking for updates...");

                var serverVersion = await CheckForInstallerVersion();
                var haveDashboard = IsDashboardInstalled();
                var localVersion = haveDashboard ? GetDashboardVersion() : null;

                var needsUpdate = !haveDashboard || IsNewerVersion(serverVersion, localVersion);

                if (needsUpdate)
                {
                    if (IsDashboardRunning())
                    {
                        if (_closeApp)
                        {
                            StopDashboard();
                            await DownloadAndInstall(serverVersion);
                        }
                        else
                        {
                            await SetError("Your Nolvus Dashboard is already running. Close it first.");
                            await ShowButton(true);
                        }
                    }
                    else
                    {
                        await DownloadAndInstall(serverVersion);
                    }
                }
                else
                {
                    await SetProgress(0);
                    await SetInfo("Your Nolvus Dashboard is up to date.");
                    await ShowButton(true);
                }
            }
            catch (Exception ex)
            {
                await SetError($"Error ({ex.Message})");
                await ShowButton(true);
            }
        }

        #region Original logic (adapted)

        private static Task InitApi() => Task.Run(() =>
        {
            ApiManager.Init("https://www.nolvus.net/rest/", "v1", string.Empty, string.Empty);
        });

        private static Task<string> CheckForInstallerVersion()
            => ApiManager.Service.Installer.GetLatestInstallerVersion();

        private static bool IsDashboardInstalled()
            => File.Exists(DashboardPath);

        private static bool IsDashboardRunning()
            => Process.GetProcessesByName("NolvusDashboard").Any();

        private static void StopDashboard()
        {
            foreach (var p in Process.GetProcessesByName("NolvusDashboard"))
            {
                try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
            }
        }

        private static void StartDashboard()
        {
            if (!IsDashboardRunning() && IsDashboardInstalled())
            {
                var p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        FileName = DashboardPath,
                        UseShellExecute = false
                    }
                };

                // ensure executable bit on Unix
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try { Process.Start("chmod", $"+x \"{DashboardPath}\"")?.WaitForExit(2000); } catch { }
                }

                try { p.Start(); } catch { /* ignore */ }
            }
        }

        private static string GetDashboardVersion()
            => ServiceSingleton.Globals.GetVersion(DashboardPath);

        private static bool IsNewerVersion(string vServer, string? vLocal)
        {
            if (string.IsNullOrWhiteSpace(vLocal)) return true;
            var s = vServer.Split('.');
            var l = vLocal.Split('.');
            for (int i = 0; i < Math.Min(s.Length, l.Length); i++)
            {
                if (!short.TryParse(s[i], out var sv)) sv = 0;
                if (!short.TryParse(l[i], out var lv)) lv = 0;
                if (sv > lv) return true;
                if (sv < lv) return false;
            }
            return s.Length > l.Length;
        }

        #endregion

        #region Download & Extract (with original-style callbacks)

        // ORIGINAL-NAMED HANDLERS (match WinForms code signatures)
        private void Downloading(object sender, Nolvus.Core.Events.DownloadProgress e)
        {
            // UI-thread safe
            Dispatcher.UIThread.Post(() =>
            {
                Log($"Progress: {e.ProgressPercentage}%");
                LblInfo.Text = $"Downloading latest Nolvus Dashboard ({e.ProgressPercentage}%)...";
                ProgressBar.Value = e.ProgressPercentage;
            });
        }

        private void Extracting(object sender, ExtractProgress e)
        {
            // UI-thread safe
            Dispatcher.UIThread.Post(() =>
            {
                LblInfo.Text = $"Installating Application ({e.ProgressPercentage}%)...";
                ProgressBar.Value = e.ProgressPercentage;
            });
        }

        private async Task DownloadAndInstall(string version)
        {
            Log($"DownloadAllInstall START version={version}");
            await SetInfo("Initializing download...");
            await SetProgress(0);

            var archivePath = Path.Combine(Path.GetTempPath(),
                "Binaries" + version.Replace(".", string.Empty) + ".zip");

            Log($"archivePath={archivePath}");

            if (File.Exists(archivePath))
            {
                try { File.Delete(archivePath); Log("Deleted old archivePath"); } catch { }
            }

            try
            {
                var fileService = new FileService();

                Log("FileService created");





                if (ApiManager.Service == null)
                {
                    Log("ApiManager.Service IS NULL");
                    await SetError("Internal Error: ApiManager failed to initialize.");
                    return;
                }

                if (ApiManager.Service.Installer == null)
                {
                    Log("ApiManager.Service.Installer IS NULL");
                    await SetError("Internal Error: Installer service not available.");
                    return;
                }




                // Keep the original behavior and delegate signature:
                // url, destination, progressHandler
                var url = await ApiManager.Service.Installer.GetLatestInstallerLink();




                Log($"Installer download url = {url ?? "<NULL>"}");

                if (string.IsNullOrWhiteSpace(url))
                {
                    Log("URL WAS NULL or EMPTY");
                    await SetError("Failed to retrieve installer download link.");
                    return;
                }


                Log("Starting download...");
                //await fileService.DownloadFile(url, archivePath, new DownloadProgressChangedHandler(Downloading));
                await fileService.DownloadFile(url, archivePath, Downloading);
                Log("Download completed.");
                Log("Starting extract...");
                await ExtractFiles(archivePath, AppDomain.CurrentDomain.BaseDirectory);
                Log("Extract completed.");

                await SetInfo("Your Nolvus Dashboard has been installed.");
                await ShowButton(true);
                Log("DownloadAndInstall FINISHED OK");
            }
            finally
            {
                try { if (File.Exists(archivePath)) File.Delete(archivePath); } catch { }
            }
        }

        private Task ExtractFiles(string fileName, string extractPath)
        {
            // Mirror the original (ZipArchive) semantics and raise Extracting with a percentage
            return Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(fileName);
                int total = archive.Entries.Count;
                int done = 0;

                foreach (var entry in archive.Entries)
                {
                    string fullPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));
                    if (!fullPath.StartsWith(Path.GetFullPath(extractPath), StringComparison.Ordinal))
                        continue;

                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                        entry.FullName.EndsWith("\\", StringComparison.Ordinal))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                        entry.ExtractToFile(fullPath, overwrite: true);
                    }

                    done++;
                    int percent = Math.Clamp((int)(100.0 * done / Math.Max(1, total)), 0, 100);
                    Extracting(this, new ExtractProgress(percent));
                }
            });
        }

        #endregion

        // DBus-safe close (don’t block the dispatcher on shutdown)
        private void BtnClose_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!_error)
            {
                if (_closeApp || IsDashboardInstalled())
                    StartDashboard();
            }

            // Post close to UI loop (no Wait/Send)
            Dispatcher.UIThread.Post(Close);
        }
    }

    // If your Nolvus.Services.Files already defines these types, REMOVE the definitions below
    // and keep the using Nolvus.Services.Files; at the top. They’re provided here only to
    // satisfy compilation if the Updater project doesn’t pull them in transitively.

    // Comment these out if duplicates occur.
    public sealed class DownloadProgress : EventArgs
    {
        public int ProgressPercentage { get; }
        public DownloadProgress(int progress) => ProgressPercentage = progress;
    }

    public sealed class ExtractProgress : EventArgs
    {
        public int ProgressPercentage { get; }
        public ExtractProgress(int progress) => ProgressPercentage = progress;
    }
        
}

