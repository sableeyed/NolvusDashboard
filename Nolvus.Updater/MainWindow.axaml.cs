using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nolvus.Updater;

public partial class MainWindow : Window
{
    // Hard-coded names (no guessing / candidates)
    private const string DashboardExeName = "NolvusDashboard";
    private const string DashboardDllName = "NolvusDashboard.dll";
    private const string UpdaterExeName = "NolvusUpdater";

    private const string RepoOwner = "sableeyed";
    private const string RepoName = "NolvusDashboard";
    private const string LatestReleaseUrl = "https://github.com/sableeyed/NolvusDashboard/releases/latest";

    private bool _error;

    private string _installDir = string.Empty;
    private string _targetVersion = string.Empty;
    private int? _dashboardPid;
    private bool _restart = true;

    public MainWindow()
    {
        InitializeComponent();

        LblInfo.Text = "Initializing...";
        ProgressBar.Value = 0;

        BtnClose.IsVisible = false;
        LblError.IsVisible = false;

        SetWindowIcon();

        Opened += (_, __) => _ = Task.Run(CheckForUpdate);
    }

    private async Task CheckForUpdate()
    {
        try
        {
            ParseArgs();

            if (string.IsNullOrWhiteSpace(_installDir))
                _installDir = Path.GetFullPath(AppContext.BaseDirectory);

            _installDir = Path.GetFullPath(_installDir);

            SetProgress(0);
            SetInfo("Check for updates...");

            if (string.IsNullOrWhiteSpace(_targetVersion))
            {
                var localVersion = GetLocalDashboardVersion(_installDir);
                var latest = await GetLatestReleaseTag();
                Console.WriteLine($"Local: {localVersion} Latest: {latest}");

                if (!ParseNolvusVersion(latest, out var remote))
                {
                    SetError($"Invalid release tag: {latest}");
                    return;
                }

                if (localVersion >= remote)
                {
                    SetProgress(0);
                    SetInfo("Your Nolvus Dashboard is up to date.");
                    ShowButton(true);
                    return;
                }

                _targetVersion = latest;
            }

            if (_dashboardPid.HasValue)
            {
                SetInfo("Waiting for Dashboard to close...");
                await WaitForProcessExit(_dashboardPid.Value, timeoutMs: 30_000);
            }

            SetProgress(10);

            var assetName = $"Binaries-{_targetVersion}.tar.gz";
            SetInfo($"Resolving download link ({_targetVersion})...");
            var assetUrl = await GetReleaseAssetUrl(RepoOwner, RepoName, _targetVersion, assetName);

            SetProgress(20);

            var workDir = Path.Combine(_installDir, ".updater");
            Directory.CreateDirectory(workDir);

            var archivePath = Path.Combine(workDir, assetName);
            if (File.Exists(archivePath))
                File.Delete(archivePath);

            SetInfo("Downloading latest Nolvus Dashboard (0%)...");
            await DownloadFile(assetUrl, archivePath, p =>
            {
                SetInfo($"Downloading latest Nolvus Dashboard ({p}%)...");
                SetProgress(20 + (int)(p * 0.40));
            });

            var extractDir = Path.Combine(workDir, $"extract-{Guid.NewGuid():N}");
            Directory.CreateDirectory(extractDir);

            SetInfo("Installating Application (0%)...");
            await Extract(archivePath, extractDir, p =>
            {
                SetInfo($"Installating Application ({p}%)...");
                SetProgress(60 + (int)(p * 0.20));
            });

            var payloadRoot = DetectPayloadRoot(extractDir);
            var backupDir = Path.Combine(_installDir, $".backup-{DateTime.UtcNow:yyyyMMddHHmmss}");
            Directory.CreateDirectory(backupDir);

            SetInfo("Finalizing update (0%)...");
            await ApplyPayloadWithBackup(payloadRoot, _installDir, backupDir, p =>
            {
                SetInfo($"Finalizing update ({p}%)...");
                SetProgress(80 + (int)(p * 0.20));
            });

            DeleteDirectory(extractDir);
            DeleteFile(archivePath);

            SetInfo("Your Nolvus Dashboard has been installed.");
            SetProgress(100);
            ShowButton(true);
        }
        catch (Exception ex)
        {
            SetError("Error (" + ex.Message + ")");
        }
    }

    private void ParseArgs()
    {
        var args = Environment.GetCommandLineArgs();

        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];

            if (a == "--install-dir" && i + 1 < args.Length)
            {
                _installDir = args[++i];
            }
            else if (a == "--version" && i + 1 < args.Length)
            {
                _targetVersion = args[++i];
            }
            else if (a == "--pid" && i + 1 < args.Length && int.TryParse(args[++i], out var pid))
            {
                _dashboardPid = pid;
            }
            else if (a == "--no-restart")
            {
                _restart = false;
            }
        }
    }

    private static async Task WaitForProcessExit(int pid, int timeoutMs)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);

            if (proc.HasExited)
                return;

            var sw = Stopwatch.StartNew();
            while (!proc.HasExited && sw.ElapsedMilliseconds < timeoutMs)
                await Task.Delay(250);

            if (!proc.HasExited)
                throw new TimeoutException("Dashboard is still running. Close it first.");
        }
        catch (ArgumentException) {}
    }


    private static Version GetLocalDashboardVersion(string installDir)
    {
        var dllPath = Path.Combine(installDir, DashboardDllName);
        if (!File.Exists(dllPath))
            return new Version(0, 0, 0, 0);

        try
        {
            var asm = Assembly.LoadFrom(dllPath);

            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (ParseNolvusVersion(info, out var vInfo))
                return vInfo;

            var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (ParseNolvusVersion(file, out var vFile))
                return vFile;

            var vAsm = AssemblyName.GetAssemblyName(dllPath).Version;
            if (vAsm != null)
            {
                // AssemblyVersion is often 3-part; normalize to 4
                return new Version(vAsm.Major, vAsm.Minor, vAsm.Build, vAsm.Revision);
            }
        }
        catch {}

        return new Version(0, 0, 0, 0);
    }

    private static bool ParseNolvusVersion(string? s, out Version version)
    {
        version = new Version(0, 0, 0, 0);

        if (string.IsNullOrWhiteSpace(s))
            return false;

        s = s.Trim();

        int i = 0;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.'))
            i++;

        var numeric = s[..i];
        if (string.IsNullOrWhiteSpace(numeric))
            return false;

        var parts = numeric.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || parts.Length > 4)
            return false;

        if (!int.TryParse(parts[0], out var a)) return false;
        if (!int.TryParse(parts[1], out var b)) return false;
        if (!int.TryParse(parts[2], out var c)) return false;

        var d = 0;
        if (parts.Length == 4 && !int.TryParse(parts[3], out d)) return false;

        version = new Version(a, b, c, d);
        return true;
    }

    private static async Task<string> GetLatestReleaseTag()
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = true };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NolvusUpdater/1.0");

        using var response = await client.GetAsync(LatestReleaseUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var uri = response.RequestMessage!.RequestUri!;
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments[^1];
    }

    private static async Task<string> GetReleaseAssetUrl(string owner, string repo, string tag, string assetName)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NolvusUpdater/1.0");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var api = $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{tag}";
        using var resp = await client.GetAsync(api, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync();
        using var doc = JsonDocument.Parse(stream);

        foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = a.GetProperty("name").GetString();
            if (string.Equals(name, assetName, StringComparison.Ordinal))
            {
                return a.GetProperty("browser_download_url").GetString()
                       ?? throw new InvalidOperationException("Missing browser_download_url");
            }
        }

        throw new FileNotFoundException($"Release asset not found: {assetName}");
    }

    private static async Task DownloadFile(string url, string destPath, Action<int> onProgress)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NolvusUpdater/1.0");

        using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength;
        await using var src = await resp.Content.ReadAsStreamAsync();
        await using var dst = File.Create(destPath);

        var buffer = new byte[128 * 1024];
        long readTotal = 0;
        int lastPct = -1;

        while (true)
        {
            var read = await src.ReadAsync(buffer, 0, buffer.Length);
            if (read <= 0)
                break;

            await dst.WriteAsync(buffer, 0, read);
            readTotal += read;

            if (total.HasValue && total.Value > 0)
            {
                var pct = (int)((readTotal * 100L) / total.Value);
                if (pct != lastPct)
                {
                    lastPct = pct;
                    onProgress(Math.Clamp(pct, 0, 100));
                }
            }
        }

        if (!total.HasValue)
            onProgress(100);
    }

    private static async Task Extract(string archivePath, string extractDir, Action<int> onProgress)
    {
        int total = 0;

        using (var fs1 = File.OpenRead(archivePath))
        using (var gz1 = new GZipStream(fs1, CompressionMode.Decompress))
        using (var r1 = new TarReader(gz1))
        {
            while (r1.GetNextEntry() != null)
                total++;
        }

        int done = 0;
        using (var fs2 = File.OpenRead(archivePath))
        using (var gz2 = new GZipStream(fs2, CompressionMode.Decompress))
        using (var r2 = new TarReader(gz2))
        {
            TarEntry? entry;
            while ((entry = r2.GetNextEntry()) != null)
            {
                var dest = Path.GetFullPath(Path.Combine(extractDir, entry.Name));

                if (!dest.StartsWith(extractDir, StringComparison.Ordinal))
                    continue;

                if (entry.EntryType == TarEntryType.Directory)
                {
                    Directory.CreateDirectory(dest);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    entry.ExtractToFile(dest, overwrite: true);
                }

                done++;
                var pct = total == 0 ? 100 : (int)((done * 100L) / total);
                onProgress(Math.Clamp(pct, 0, 100));
            }
        }

        await Task.CompletedTask;
    }

    private static string DetectPayloadRoot(string extractDir)
    {
        var entries = Directory.GetFileSystemEntries(extractDir);
        if (entries.Length == 1 && Directory.Exists(entries[0]))
            return entries[0];

        return extractDir;
    }

    private static async Task ApplyPayloadWithBackup(string payloadDir, string installDir, string backupDir, Action<int> onProgress)
    {
        var files = Directory.EnumerateFiles(payloadDir, "*", SearchOption.AllDirectories).ToList();
        int total = files.Count;
        int done = 0;

        foreach (var srcPath in files)
        {
            var rel = Path.GetRelativePath(payloadDir, srcPath);
            var dstPath = Path.Combine(installDir, rel);

            Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);

            if (File.Exists(dstPath))
            {
                var backupPath = Path.Combine(backupDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.Move(dstPath, backupPath, overwrite: true);
            }

            File.Copy(srcPath, dstPath, overwrite: true);

            done++;
            var pct = total == 0 ? 100 : (int)((done * 100L) / total);
            onProgress(Math.Clamp(pct, 0, 100));
        }

        await SetExecutable(Path.Combine(installDir, UpdaterExeName));
        await SetExecutable(Path.Combine(installDir, DashboardExeName));
    }

    private static async Task SetExecutable(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "chmod",
                UseShellExecute = false
            };
            psi.ArgumentList.Add("+x");
            psi.ArgumentList.Add(path);

            using var p = Process.Start(psi);
            if (p != null)
                await p.WaitForExitAsync();
        }
        catch {}
    }

    private void BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        if (!_error && _restart)
        {
            try
            {
                var dashboardExe = Path.Combine(_installDir, DashboardExeName);
                var dashboardDll = Path.Combine(_installDir, DashboardDllName);

                var psi = new ProcessStartInfo
                {
                    WorkingDirectory = _installDir,
                    UseShellExecute = false
                };

                if (File.Exists(dashboardExe))
                {
                    psi.FileName = dashboardExe;
                }
                else if (File.Exists(dashboardDll))
                {
                    psi.FileName = "dotnet";
                    psi.ArgumentList.Add(dashboardDll);
                }
                else
                {
                    Close();
                    return;
                }

                Process.Start(psi);
            }
            catch {}
        }

        Close();
    }

    public void SetInfo(string value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetInfo(value));
            return;
        }

        LblInfo.Text = value;
    }

    public void SetProgress(int value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetProgress(value));
            return;
        }

        ProgressBar.Value = value;
    }

    public void ShowButton(bool value)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ShowButton(value));
            return;
        }

        BtnClose.IsVisible = value;
    }

    public void SetError(string value)
    {
        _error = true;

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetError(value));
            return;
        }

        LblInfo.Text = string.Empty;
        SetProgress(0);

        LblError.IsVisible = true;
        LblError.Text = value;

        ShowButton(true);
    }

    private static void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch { }
    }

    private void SetWindowIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(
                new Uri("avares://Nolvus.Updater/Assets/nolvus-ico.jpg"));
            Icon = new WindowIcon(new Bitmap(stream));
        }
        catch {}
    }
}
