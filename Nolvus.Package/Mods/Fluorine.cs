using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Nolvus.Core.Errors;
using Nolvus.Core.Events;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.NexusApi;
using Nolvus.Package.Utilities;

namespace Nolvus.Package.Mods
{
    public static class Fluorine
    {
        public const string Domain = "site";
        public const int NexusId = 1997;
        public const string GitHubRepository = "SulfurNitride/Fluorine-Manager";
        private const string GitHubLatestReleaseApi = "https://api.github.com/repos/" + GitHubRepository + "/releases/latest";
        private const string GitHubReleasePage = "https://github.com/" + GitHubRepository + "/releases/latest";
        private const string GitHubAssetName = "Fluorine-Manager.zip";
        private const string ReleaseMarker = "nolvus-fluorine-release.txt";

        public static string ModPage
        {
            get { return $"https://www.nexusmods.com/{Domain}/mods/{NexusId}"; }
        }

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

        private static string MarkerFile
        {
            get { return Path.Combine(InstallDirectory, ReleaseMarker); }
        }

        public static bool IsInstalled
        {
            get { return File.Exists(Executable) && File.Exists(MarkerFile); }
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

        /// <summary>
        /// Installs Fluorine Manager if it is not already there, latest release first from Nexus
        /// and from the project's GitHub releases if Nexus cannot provide it.
        /// </summary>
        /// <remarks>
        /// This runs on a fresh list install, on the upgrade from Mod Organizer 2, and behind the
        /// play button. All three are the same case : put a complete install in place, or leave the
        /// one that is already there alone. Fluorine updates itself in place once installed, so the
        /// dashboard replacing it would clobber a newer build it had updated itself to, along with
        /// whatever its overlay update preserves in bin/.
        ///
        /// Fluorine is not part of the Nolvus package, so it has no entry in the mod list and no
        /// NexusModFile to go through, but it is fetched the same way every other Nexus file is :
        /// premium accounts get a CDN link straight from the API, free accounts resolve one through
        /// the browser, and the download goes through the shared file service either way. A failure
        /// anywhere in that path - the lookup, the link, the download, the extract - falls back to
        /// GitHub, which covers a Nexus outage, a rate limited key, and a free account with no
        /// browser to resolve a link with.
        ///
        /// <paramref name="Browser"/> may be left null when the caller has no browser to offer.
        /// </remarks>
        public static async Task Install(DownloadProgressChangedHandler OnDownload, ExtractProgressChangedHandler OnExtract, Func<IBrowserInstance> Browser = null)
        {
            if (IsInstalled)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] Release {GetInstalledRelease()} already installed in {InstallDirectory}, skipping download");
                return;
            }

            try
            {
                await DownloadAndInstall(await GetNexusDownload(Browser), OnDownload, OnExtract);

                return;
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] Unable to install from {ModPage} ({ex.Message}), falling back to {GitHubReleasePage}");
            }

            try
            {
                await DownloadAndInstall(await GetGitHubDownload(), OnDownload, OnExtract);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to download Fluorine Manager : " + ex.Message, ex);
            }
        }

        /// <summary>
        /// A release to install : where to get it, what the archive is called, and what is recorded
        /// in the marker file to say which release the install came from.
        /// </summary>
        private sealed class Download
        {
            public string Link;
            public string FileName;
            public string Release;
            public string Source;
        }

        private static async Task DownloadAndInstall(Download Download, DownloadProgressChangedHandler OnDownload, ExtractProgressChangedHandler OnExtract)
        {
            ServiceSingleton.Logger.Log($"[FLUORINE] Installing {Download.Release} from {Download.Source}");

            var Archive = Path.Combine(ServiceSingleton.Folders.DownloadDirectory, Download.FileName);

            try
            {
                await ServiceSingleton.Files.DownloadFile(Download.Link, Archive, OnDownload);

                // Only ever reached with nothing properly installed, so this clears whatever an
                // interrupted attempt left behind rather than a working install. The wine prefix
                // sits beside this directory rather than inside it, so it survives.
                ServiceSingleton.Files.RemoveDirectory(InstallDirectory, true);

                Directory.CreateDirectory(InstallDirectory);

                await ServiceSingleton.Files.ExtractFile(Archive, InstallDirectory, OnExtract);

                EnsureExecutable();

                if (!File.Exists(Executable))
                    throw new FileNotFoundException($"{Download.FileName} did not contain Fluorine Manager", Executable);

                // Written last, so anything that fails before this point leaves no marker and does
                // not count as installed - see IsInstalled.
                File.WriteAllText(MarkerFile, Download.Release);

                ServiceSingleton.Logger.Log($"[FLUORINE] {Download.Release} installed in {InstallDirectory}");
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

        private static async Task<Download> GetNexusDownload(Func<IBrowserInstance> Browser)
        {
            var Latest = await GetLatestFile();

            return new Download
            {
                Link = await GetDownloadLink(Latest, Browser),
                FileName = Latest.FileName,
                Release = Latest.FileID.ToString(),
                Source = ModPage
            };
        }

        /// <summary>
        /// The latest GitHub release, used only as a fallback for a first install.
        /// </summary>
        /// <remarks>
        /// The marker keeps the release tag here rather than a Nexus file id. IsUpToDate compares
        /// on version as well as file id for exactly this reason, so an install that came from
        /// GitHub is not re-downloaded from Nexus the next time round.
        /// </remarks>
        private static async Task<Download> GetGitHubDownload()
        {
            using var Client = new HttpClient();

            // GitHub rejects API requests that do not identify themselves.
            Client.DefaultRequestHeaders.Add("User-Agent", "NolvusDashboard");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            using var Document = JsonDocument.Parse(await Client.GetStringAsync(GitHubLatestReleaseApi));

            var Tag = Document.RootElement.GetProperty("tag_name").GetString();

            var Asset = Document.RootElement
                .GetProperty("assets")
                .EnumerateArray()
                .FirstOrDefault(x => x.GetProperty("name").GetString() == GitHubAssetName);

            if (Asset.ValueKind == JsonValueKind.Undefined)
                throw new Exception($"Release {Tag} does not contain {GitHubAssetName}");

            return new Download
            {
                Link = Asset.GetProperty("browser_download_url").GetString(),
                FileName = GitHubAssetName,
                Release = Tag,
                Source = GitHubReleasePage
            };
        }

        /// <summary>
        /// The newest file the Nexus page publishes, which is what "latest Fluorine" means here -
        /// the mod has no version endpoint of its own worth trusting over its uploads.
        /// </summary>
        private static async Task<NexusApi.Responses.ModFile> GetLatestFile()
        {
            // Filtering by category makes ApiManager block on the request internally, so it is
            // kept off the UI thread - the play button calls Install from its click handler.
            var Files = await Task.Run(() => ApiManager.GetModFiles(Domain, NexusId, NexusApi.Responses.FileCategory.Main));

            // Fall back to whatever else is published if nothing is flagged as a main file.
            if (Files.Length == 0)
            {
                Files = await Task.Run(() => ApiManager.GetModFiles(Domain, NexusId,
                    NexusApi.Responses.FileCategory.Update,
                    NexusApi.Responses.FileCategory.Optional,
                    NexusApi.Responses.FileCategory.Miscellaneous));
            }

            var Latest = Files.OrderByDescending(x => x.UploadedTimestamp).FirstOrDefault();

            if (Latest == null)
                throw new Exception($"No downloadable file found on {ModPage}");

            return Latest;
        }

        private static async Task<string> GetDownloadLink(NexusApi.Responses.ModFile File_, Func<IBrowserInstance> Browser)
        {
            if (ApiManager.AccountInfo.IsPremium)
            {
                var Links = await ApiManager.GetDownloadLinks(Domain, NexusId, File_.FileID);

                // Fluorine can be installed from the play button, where there is no working
                // instance to take a CDN preference from, so any link will do as a fallback.
                var Cdn = ServiceSingleton.Instances.WorkingInstance?.Settings?.CDN;

                var Link = Links.FirstOrDefault(x => x.ShortName == Cdn) ?? Links.FirstOrDefault();

                if (Link == null)
                    throw new Exception($"Nexus returned no download link for {File_.FileName}");

                return Link.Uri.ToString();
            }

            if (Browser == null)
                throw new Exception($"A free Nexus account has to download {File_.FileName} through the browser, which is not available here");

            var ManualLink = $"{ModPage}?tab=files&file_id={File_.FileID}&nmm=0";

            // Shares the gate with the mod installs so only ever one browser window is on screen,
            // which matters because Fluorine is fetched while the mod list is installing.
            return await BrowserGate.RunAsync($"manual link for {File_.FileName}", async () =>
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] Awaiting manual user download link for file {File_.FileName}");

                var browserTries = 0;

                while (true)
                {
                    try
                    {
                        return await Browser().GetNexusManualDownloadLink("Fluorine Manager", ManualLink, NexusId.ToString()).ConfigureAwait(false);
                    }
                    catch (BrowserClosedException) when (browserTries < ServiceSingleton.Settings.RetryCount)
                    {
                        browserTries++;
                        ServiceSingleton.Logger.Log($"[FLUORINE] Manual download window closed before completing, reopening ({browserTries}/{ServiceSingleton.Settings.RetryCount}) for file {File_.FileName}");
                    }
                }
            }).ConfigureAwait(false);
        }

        private static string GetInstalledRelease()
        {
            try
            {
                return File.Exists(MarkerFile) ? File.ReadAllText(MarkerFile).Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

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
