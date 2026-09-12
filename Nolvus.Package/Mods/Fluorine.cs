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
        /// <summary>
        /// Fluorine's own wine prefix. It does not necessarily exist - on a first install there is
        /// nothing here at all, and Fluorine rebuilds it whenever it finds it missing or invalid
        /// (it validates by looking for drive_c) - so nothing may assume it is present.
        /// </summary>
        public static string PrefixDirectory
        {
            get { return Path.Combine(Path.GetDirectoryName(InstallDirectory), "Prefix", "pfx"); }
        }

        /// <summary>
        /// Points X: at the instance directory. The ini addresses everything in the instance
        /// relative to X: to stay inside MAX_PATH, so without this the instance resolves to nothing.
        /// Returns false when the prefix is not there yet, which is not an error - it just means
        /// Fluorine has not built it.
        /// </summary>
        public static bool PrefixExists
        {
            get { return Directory.Exists(Path.Combine(PrefixDirectory, "dosdevices")); }
        }

        public static bool TryMapInstanceDrive(string InstallDir)
        {
            if (string.IsNullOrWhiteSpace(InstallDir))
                return false;

            var DosDevices = Path.Combine(PrefixDirectory, "dosdevices");

            if (!Directory.Exists(DosDevices))
                return false;

            var Drive = Path.Combine(DosDevices, "x:");

            try
            {
                var Current = new FileInfo(Drive).LinkTarget;

                if (string.Equals(Current, InstallDir, StringComparison.Ordinal))
                    return true;

                // Path.Exists covers a working link, a dangling one and a real directory; any of
                // them would make CreateSymbolicLink throw. File.Delete unlinks without following.
                if (Path.Exists(Drive) || Current != null)
                    File.Delete(Drive);

                File.CreateSymbolicLink(Drive, InstallDir);

                ServiceSingleton.Logger.Log($"[FLUORINE] Mapped X: -> {InstallDir} in {PrefixDirectory}");

                return true;
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] Could not map X: in {PrefixDirectory} : {ex.Message}");

                return false;
            }
        }

        public static Process Start(string InstallDir = null)
        {
            if (!IsInstalled)
                throw new FileNotFoundException("Fluorine Manager is not installed", Executable);

            if (!string.IsNullOrWhiteSpace(InstallDir))
            {
                // Cheap re-assert so X: follows the instance being launched; with more than one
                // instance installed it would otherwise still point at whichever was configured
                // last. Silent when the prefix is absent - "Configure Fluorine Prefix" is where the
                // user gets told about that.
                TryMapInstanceDrive(InstallDir);
            }

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
            string AssetUrl;

            try
            {
                (Tag, AssetUrl) = await GetLatestRelease();
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to download Fluorine Manager : " + ex.Message, ex);
            }

            ServiceSingleton.Logger.Log($"[FLUORINE] Installing {Tag} from {AssetUrl}");

            Directory.CreateDirectory(InstallDirectory);

            var Archive = Path.Combine(ServiceSingleton.Folders.DownloadDirectory, AssetName);

            try
            {
                await ServiceSingleton.Files.DownloadFile(AssetUrl, Archive, OnDownload);

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

        private static async Task<(string Tag, string AssetUrl)> GetLatestRelease()
        {
            using var Client = new HttpClient();

            Client.DefaultRequestHeaders.Add("User-Agent", "NolvusDashboard");
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

            return (Tag, Asset.GetProperty("browser_download_url").GetString());
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
