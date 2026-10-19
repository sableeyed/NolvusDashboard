using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Nolvus.Core.Enums;
using Nolvus.Core.Errors;
using Nolvus.Core.Events;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Utils;
using Nolvus.NexusApi;
using Nolvus.Package.Utilities;

namespace Nolvus.Package.Mods
{
    public class FluorineManager : Software
    {
        private const string Domain = "site";
        private const int NexusId = 1997;
        private const string GitHubRepository = "SulfurNitride/Fluorine-Manager";
        private const string GitHubLatestReleaseApi = "https://api.github.com/repos/" + GitHubRepository + "/releases/latest";
        private const string GitHubReleasePage = "https://github.com/" + GitHubRepository + "/releases/latest";

        private Release Fetched;

        private string ExtractedRoot;

        private static string ModPage
        {
            get { return $"https://www.nexusmods.com/{Domain}/mods/{NexusId}"; }
        }

        private static string StagingDirectory
        {
            get { return Fluorine.InstallDirectory + ".extract"; }
        }

        private string ArchiveFile
        {
            get { return Path.Combine(ServiceSingleton.Folders.DownloadDirectory, Fetched.FileName); }
        }

        public FluorineManager()
        {
            Name = "Fluorine Manager";
            Author = "SulfurNitride";
            // The release is only known once Nexus or GitHub has been asked for it.
            Version = "latest";
            ImagePath = string.Empty;
            Action = ElementAction.Add;
        }

        public void Load(List<InstallableElement> Elements)
        {
            Index = Elements.Count;

            Elements.Add(this);
        }

        private sealed class Release
        {
            public string Link;
            public string FileName;
            public string Id;
            public string Source;
        }

        protected override async Task DoDownload(Func<IBrowserInstance> Browser)
        {
            Fetched = null;

            if (Fluorine.IsInstalled)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] Already installed in {Fluorine.InstallDirectory}, skipping download");
                return;
            }

            try
            {
                await Fetch(await GetNexusRelease(Browser));
            }
            catch (Exception ex)
            {
                await FallBackToGitHub(ex);
            }
        }

        protected override async Task DoExtract()
        {
            if (Fetched == null)
                return;

            try
            {
                await Extract();
            }
            catch (Exception ex) when (Fetched.Source == ModPage)
            {
                await FallBackToGitHub(ex);
                await Extract();
            }
        }

        protected override async Task DoCopy()
        {
            if (Fetched == null)
                return;

            await Task.Run(() =>
            {
                try
                {
                    ServiceSingleton.Files.RemoveDirectory(Fluorine.InstallDirectory, true);

                    Directory.Move(ExtractedRoot, Fluorine.InstallDirectory);

                    EnsureExecutable();

                    File.WriteAllText(Fluorine.MarkerFile, Fetched.Id);

                    CopyingProgress(1, 1);

                    ServiceSingleton.Logger.Log($"[FLUORINE] {Fetched.Id} installed in {Fluorine.InstallDirectory}");
                }
                finally
                {
                    ServiceSingleton.Files.RemoveDirectory(StagingDirectory, true);
                }
            });
        }

        // Fluorine belongs to the machine rather than the instance, and an install already in place
        // is never replaced, so there is nothing to keep the archive in the instance's archive for.
        protected override Task DoArchive()
        {
            DeleteArchive();

            return Task.CompletedTask;
        }

        private async Task Fetch(Release Latest)
        {
            ServiceSingleton.Logger.Log($"[FLUORINE] Installing {Latest.Id} from {Latest.Source}");

            Fetched = Latest;

            await ServiceSingleton.Files.DownloadFile(Latest.Link, ArchiveFile, DownloadingProgress);
        }

        private async Task FallBackToGitHub(Exception Reason)
        {
            ServiceSingleton.Logger.Log($"[FLUORINE] Unable to install from {ModPage} ({Reason.Message}), falling back to {GitHubReleasePage}");

            // Whatever Nexus left half downloaded or unusable would otherwise stay in the cache.
            DeleteArchive();

            try
            {
                await Fetch(await GetGitHubRelease());
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to download Fluorine Manager : " + ex.Message, ex);
            }
        }

        private async Task Extract()
        {
            try
            {
                ServiceSingleton.Files.RemoveDirectory(StagingDirectory, true);

                if (new[] { ".7z", ".rar", ".zip" }.Contains(Path.GetExtension(Fetched.FileName), StringComparer.OrdinalIgnoreCase))
                    await ServiceSingleton.Files.ExtractFile(ArchiveFile, StagingDirectory, ExtractingProgress);
                else
                    await ExtractWithTar(ArchiveFile, StagingDirectory);

                ExtractedRoot = new[] { StagingDirectory }
                    .Concat(Directory.GetDirectories(StagingDirectory))
                    .FirstOrDefault(x => File.Exists(Path.Combine(x, "fluorine-manager")));

                if (ExtractedRoot == null)
                    throw new FileNotFoundException($"{Fetched.FileName} did not contain Fluorine Manager", Fluorine.Executable);
            }
            catch
            {
                ServiceSingleton.Files.RemoveDirectory(StagingDirectory, true);
                throw;
            }
        }

        private async Task ExtractWithTar(string Archive, string Output)
        {
            var Tar = ExecutableResolver.RequireExecutable("tar");

            Directory.CreateDirectory(Output);

            ExtractingProgress(this, new ExtractProgress { FileName = Path.GetFileName(Archive), ProgressPercentage = 0 });

            var psi = new ProcessStartInfo
            {
                FileName = Tar,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            psi.ArgumentList.Add("-xf");
            psi.ArgumentList.Add(Archive);
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(Output);

            using var p = Process.Start(psi) ?? throw new Exception($"Unable to start {Tar}");

            var Error = await p.StandardError.ReadToEndAsync();

            await p.WaitForExitAsync();

            if (p.ExitCode != 0)
                throw new Exception($"{Tar} could not extract {Path.GetFileName(Archive)} (exit code {p.ExitCode}) : {Error.Trim()}");
        }

        private void DeleteArchive()
        {
            if (Fetched == null)
                return;

            try
            {
                if (File.Exists(ArchiveFile))
                    File.Delete(ArchiveFile);
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[FLUORINE] Could not delete {ArchiveFile} : {ex.Message}");
            }
        }

        private async Task<Release> GetNexusRelease(Func<IBrowserInstance> Browser)
        {
            var Latest = await GetLatestFile();

            return new Release
            {
                Link = await GetDownloadLink(Latest, Browser),
                FileName = Latest.FileName,
                Id = Latest.FileID.ToString(),
                Source = ModPage
            };
        }

        private static async Task<Release> GetGitHubRelease()
        {
            using var Client = new HttpClient();

            // GitHub rejects API requests that do not identify themselves.
            Client.DefaultRequestHeaders.Add("User-Agent", "NolvusDashboard");
            Client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            using var Document = JsonDocument.Parse(await Client.GetStringAsync(GitHubLatestReleaseApi));

            var Tag = Document.RootElement.GetProperty("tag_name").GetString();

            static bool Matches(JsonElement Asset, string Extension) =>
                Asset.GetProperty("name").GetString() is string Name &&
                Name.Contains("fluorine", StringComparison.OrdinalIgnoreCase) &&
                Name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

            var Assets = Document.RootElement.GetProperty("assets").EnumerateArray().ToList();

            var Asset = Assets.FirstOrDefault(x => Matches(x, ".zip"));

            if (Asset.ValueKind == JsonValueKind.Undefined)
                Asset = Assets.FirstOrDefault(x => Matches(x, ".tar.gz"));

            if (Asset.ValueKind == JsonValueKind.Undefined)
                throw new Exception($"Release {Tag} does not contain a Fluorine Manager zip or tarball");

            return new Release
            {
                Link = Asset.GetProperty("browser_download_url").GetString(),
                FileName = Asset.GetProperty("name").GetString(),
                Id = Tag,
                Source = GitHubReleasePage
            };
        }

        private static async Task<NexusApi.Responses.ModFile> GetLatestFile()
        {
            // Filtering by category makes ApiManager block on the request internally, so it is
            // kept off the UI thread - the play button installs from its click handler.
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

        private async Task<string> GetDownloadLink(NexusApi.Responses.ModFile File_, Func<IBrowserInstance> Browser)
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
                        return await Browser().GetNexusManualDownloadLink(Name, ManualLink, NexusId.ToString()).ConfigureAwait(false);
                    }
                    catch (BrowserClosedException) when (browserTries < ServiceSingleton.Settings.RetryCount)
                    {
                        browserTries++;
                        ServiceSingleton.Logger.Log($"[FLUORINE] Manual download window closed before completing, reopening ({browserTries}/{ServiceSingleton.Settings.RetryCount}) for file {File_.FileName}");
                    }
                }
            }).ConfigureAwait(false);
        }

        private static void EnsureExecutable()
        {
            foreach (var Name in new[] { "fluorine-manager", "clf3", "ModOrganizer-core", "7zz" })
            {
                var Path_ = Path.Combine(Fluorine.InstallDirectory, Name);

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
