using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Nolvus.Core.Events;
using Nolvus.Core.Services;

namespace Nolvus.Package.Mods
{
    public static class Fluorine
    {
        public const string Repository = "SulfurNitride/Fluorine-Manager";
        private const string LatestReleaseApi = "https://api.github.com/repos/" + Repository + "/releases/latest";
        private const string AssetName = "Fluorine-Manager.zip";
        private const string ReleaseMarker = "nolvus-fluorine-release.txt";

        public static string InstallDirectory
        {
            get
            {
                var DataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

                if (string.IsNullOrWhiteSpace(DataHome))
                    DataHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

                return Path.Combine(DataHome, "fluorine", "bin");
            }
        }

        public static string Executable
        {
            get { return Path.Combine(InstallDirectory, "fluorine-manager"); }
        }

        public static bool IsInstalled
        {
            get { return File.Exists(Executable); }
        }

        // fluorine-manager is a bash wrapper that ends in `exec ModOrganizer-core`, so the wrapper
        // name is gone from the process table once it is up - match the core instead.
        public static bool IsRunning
        {
            get
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "pgrep",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add(Path.Combine(InstallDirectory, "ModOrganizer-core"));

                try
                {
                    using var p = Process.Start(psi);

                    if (p == null)
                        return false;

                    p.WaitForExit();

                    return p.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Native Linux binary, so no wine. The instance it opens comes from
        // ~/.config/Mod Organizer Team/Mod Organizer.conf, not from anything passed here.
        public static Process Start()
        {
            if (!IsInstalled)
                throw new FileNotFoundException("Fluorine Manager is not installed", Executable);

            var psi = new ProcessStartInfo
            {
                FileName = Executable,
                WorkingDirectory = InstallDirectory,
                UseShellExecute = false
            };

            ServiceSingleton.Logger.Log($"[FLUORINE] Launching {Executable}");

            return Process.Start(psi);
        }

        public static async Task Install(DownloadProgressChangedHandler OnDownload, ExtractProgressChangedHandler OnExtract)
        {
            // An existing install is left alone whatever version it is
            if (IsInstalled)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] Already installed in {InstallDirectory}, skipping download");
                return;
            }

            string Tag;
            string ApiUrl;
            string BrowserUrl;

            try
            {
                (Tag, ApiUrl, BrowserUrl) = await GetLatestRelease();
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to download Fluorine Manager : " + ex.Message, ex);
            }

            ServiceSingleton.Logger.Log($"[FLUORINE] Installing {Tag} from {ApiUrl}");

            Directory.CreateDirectory(InstallDirectory);

            var Archive = Path.Combine(ServiceSingleton.Folders.DownloadDirectory, AssetName);

            try
            {
                try
                {
                    await DownloadAsset(ApiUrl, Archive, OnDownload);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log($"[FLUORINE] API asset download failed ({ex.Message}), trying {BrowserUrl}");

                    await ServiceSingleton.Files.DownloadFile(BrowserUrl, Archive, OnDownload);
                }

                await ServiceSingleton.Files.ExtractFile(Archive, InstallDirectory, OnExtract);

                EnsureExecutable();

                File.WriteAllText(Path.Combine(InstallDirectory, ReleaseMarker), Tag);

                ServiceSingleton.Logger.Log($"[FLUORINE] {Tag} installed in {InstallDirectory}");
            }
            finally
            {
                try
                {
                    if (File.Exists(Archive))
                        File.Delete(Archive);
                }
                catch { }
            }
        }

        private static HttpClient CreateClient()
        {
            var Client = new HttpClient();

            Client.DefaultRequestHeaders.Add("User-Agent", "NolvusDashboard");

            return Client;
        }

        private static async Task<(string Tag, string ApiUrl, string BrowserUrl)> GetLatestRelease()
        {
            using var Client = CreateClient();

            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            var Json = await Client.GetStringAsync(LatestReleaseApi);

            using var Document = JsonDocument.Parse(Json);

            var Tag = Document.RootElement.GetProperty("tag_name").GetString();

            var Asset = Document.RootElement
                .GetProperty("assets")
                .EnumerateArray()
                .FirstOrDefault(x => x.GetProperty("name").GetString() == AssetName);

            if (Asset.ValueKind == JsonValueKind.Undefined)
                throw new Exception($"Release {Tag} does not contain {AssetName}");

            return (Tag, Asset.GetProperty("url").GetString(), Asset.GetProperty("browser_download_url").GetString());
        }

        private static async Task DownloadAsset(string Url, string Destination, DownloadProgressChangedHandler OnDownload)
        {
            using var Client = CreateClient();
            using var Request = new HttpRequestMessage(HttpMethod.Get, Url);

            Request.Headers.Accept.ParseAdd("application/octet-stream");

            using var Response = await Client.SendAsync(Request, HttpCompletionOption.ResponseHeadersRead);

            Response.EnsureSuccessStatusCode();

            var Progress = new DownloadProgress
            {
                FileName = Path.GetFileName(Destination),
                TotalBytesToReceive = Response.Content.Headers.ContentLength ?? -1
            };

            Progress.TotalBytesToReceiveAsString = ToMegabytes(Progress.TotalBytesToReceive);

            var Watch = Stopwatch.StartNew();

            await using var Input = await Response.Content.ReadAsStreamAsync();
            await using var Output = File.Open(Destination, FileMode.Create, FileAccess.Write, FileShare.None);

            var Buffer = new byte[81920];
            var LastPercent = -1;
            int Read;

            while ((Read = await Input.ReadAsync(Buffer)) > 0)
            {
                await Output.WriteAsync(Buffer.AsMemory(0, Read));

                Progress.BytesReceived += Read;

                var Percent = Progress.TotalBytesToReceive > 0
                    ? (int)(Progress.BytesReceived * 100 / Progress.TotalBytesToReceive)
                    : 0;

                // One UI update per percent rather than per 80KB chunk.
                if (Percent == LastPercent)
                    continue;

                LastPercent = Percent;

                Progress.ProgressPercentage = Percent;
                Progress.BytesReceivedAsString = ToMegabytes(Progress.BytesReceived);
                Progress.Speed = Progress.BytesReceived / 1024d / 1024d / Math.Max(Watch.Elapsed.TotalSeconds, 0.001);

                OnDownload?.Invoke(null, Progress);
            }

            if (Progress.TotalBytesToReceive > 0 && Progress.BytesReceived != Progress.TotalBytesToReceive)
                throw new IOException($"Download truncated : {Progress.BytesReceived} of {Progress.TotalBytesToReceive} bytes");
        }

        private static string ToMegabytes(long Bytes)
        {
            return (Bytes / 1024d / 1024d).ToString("0.00");
        }

        // Unused while an existing install is always kept. Restore alongside a version comparison
        // in Install() if update checking is reintroduced.
        //private static string GetInstalledRelease()
        //{
        //    var Marker = Path.Combine(InstallDirectory, ReleaseMarker);
        //
        //    try
        //    {
        //        return File.Exists(Marker) ? File.ReadAllText(Marker).Trim() : string.Empty;
        //    }
        //    catch
        //    {
        //        return string.Empty;
        //    }
        //}

        private static void EnsureExecutable()
        {
            foreach (var Name in new[] { "fluorine-manager", "clf3", "ModOrganizer-core", "7zz" })
            {
                var Path_ = Path.Combine(InstallDirectory, Name);

                try
                {
                    if (!File.Exists(Path_))
                        continue;

                    var Mode = File.GetUnixFileMode(Path_);

                    File.SetUnixFileMode(Path_,
                        Mode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log($"[FLUORINE] Could not mark {Name} executable : {ex.Message}");
                }
            }
        }
    }
}
