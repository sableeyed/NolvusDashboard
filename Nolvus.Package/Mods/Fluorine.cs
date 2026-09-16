using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Downloads and installs the latest Fluorine Manager from Nexus, updating an existing
        /// install when a newer file has been uploaded.
        /// </summary>
        /// <remarks>
        /// Fluorine is not part of the Nolvus package, so it has no entry in the mod list and no
        /// NexusModFile to go through. It is fetched the same way every other Nexus file is though:
        /// premium accounts get a CDN link straight from the API, free accounts resolve one through
        /// the browser, and the download itself goes through the shared file service either way.
        /// <paramref name="Browser"/> may be left null when the caller has no browser to offer, in
        /// which case a free account cannot install and an existing install is kept as it is.
        /// </remarks>
        public static async Task Install(DownloadProgressChangedHandler OnDownload, ExtractProgressChangedHandler OnExtract, Func<IBrowserInstance> Browser = null)
        {
            NexusApi.Responses.ModFile Latest;

            try
            {
                Latest = await GetLatestFile();
            }
            catch (Exception ex)
            {
                // An existing install is still playable when Nexus cannot be reached, so only a
                // first install is worth failing over.
                if (IsInstalled)
                {
                    ServiceSingleton.Logger.Log($"[FLUORINE] Unable to look up the latest version ({ex.Message}), keeping the install in {InstallDirectory}");
                    return;
                }

                throw new Exception("Unable to download Fluorine Manager : " + ex.Message, ex);
            }

            var Release = ReleaseName(Latest);

            if (IsInstalled)
            {
                if (IsUpToDate(Latest))
                {
                    ServiceSingleton.Logger.Log($"[FLUORINE] {Release} already installed in {InstallDirectory}, skipping download");
                    return;
                }

                // Replacing the binaries out from under a running Fluorine would break it mid
                // session, so the update waits until the next time it is closed.
                if (IsRunning)
                {
                    ServiceSingleton.Logger.Log($"[FLUORINE] {Release} is available but Fluorine Manager is running, leaving {InstallDirectory} alone");
                    return;
                }

                ServiceSingleton.Logger.Log($"[FLUORINE] Updating {GetInstalledRelease()} to {Release}");
            }

            string Link;

            try
            {
                Link = await GetDownloadLink(Latest, Browser);
            }
            catch (Exception ex)
            {
                if (IsInstalled)
                {
                    ServiceSingleton.Logger.Log($"[FLUORINE] Unable to get a download link for {Release} ({ex.Message}), keeping the install in {InstallDirectory}");
                    return;
                }

                throw new Exception("Unable to download Fluorine Manager : " + ex.Message, ex);
            }

            ServiceSingleton.Logger.Log($"[FLUORINE] Installing {Release} from {ModPage}");

            var Archive = Path.Combine(ServiceSingleton.Folders.DownloadDirectory, Latest.FileName);

            try
            {
                await ServiceSingleton.Files.DownloadFile(Link, Archive, OnDownload);

                // An update ships different binaries under the same names, and anything the old
                // release left behind is not wanted. The wine prefix sits beside this directory
                // rather than inside it, so it survives.
                ServiceSingleton.Files.RemoveDirectory(InstallDirectory, true);

                Directory.CreateDirectory(InstallDirectory);

                await ServiceSingleton.Files.ExtractFile(Archive, InstallDirectory, OnExtract);

                EnsureExecutable();

                File.WriteAllText(Path.Combine(InstallDirectory, ReleaseMarker), Latest.FileID.ToString());

                ServiceSingleton.Logger.Log($"[FLUORINE] {Release} installed in {InstallDirectory}");
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

            var ManualLink = $"{ModPage}?tab=files&file_id={File_.FileID}&nmm=1";

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

        private static string ReleaseName(NexusApi.Responses.ModFile File_)
        {
            return $"{File_.Version} (file {File_.FileID})";
        }

        /// <summary>
        /// Whether the install in <see cref="InstallDirectory"/> is already the given Nexus file.
        /// </summary>
        /// <remarks>
        /// The marker holds the Nexus file id, so a file re-uploaded under an unchanged version
        /// still counts as an update. Installs predating the move to Nexus left the GitHub release
        /// tag there instead, which is recognised by version so they are not made to re-download a
        /// couple of hundred megabytes of the release they already have.
        /// </remarks>
        private static bool IsUpToDate(NexusApi.Responses.ModFile Latest)
        {
            var Installed = GetInstalledRelease();

            if (Installed == string.Empty)
                return false;

            return Installed == Latest.FileID.ToString() ||
                   Installed.TrimStart('v', 'V') == (Latest.Version ?? string.Empty).TrimStart('v', 'V');
        }

        private static string GetInstalledRelease()
        {
            var Marker = Path.Combine(InstallDirectory, ReleaseMarker);

            try
            {
                return File.Exists(Marker) ? File.ReadAllText(Marker).Trim() : string.Empty;
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
