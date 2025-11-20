using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Services;
using Nolvus.Package.Rules;

namespace Nolvus.Package.Mods
{
    public class BSArch : NexusSoftware
    {
        protected override async Task DoCopy()
        {
            var instance = ServiceSingleton.Instances.WorkingInstance;

            // TOOLS/BSArch
            var installDirectory = Path.Combine(instance.InstallDir, "TOOLS", Name);
            Directory.CreateDirectory(installDirectory);

            // Extract root: .../Extract/<ExtractSubDir>
            var extractDir = Path.Combine(ServiceSingleton.Folders.ExtractDirectory, ExtractSubDir);

            try
            {
                //
                // BSArch has no XML <Rules>, so we synthesize a single DirectoryCopy rule
                // that copies EVERYTHING from extractDir into installDirectory.
                //
                // We rely on CopyRule defaults:
                //   - Destination (int) defaults to 0 → "modDir" (installDirectory)
                //   - CopyToRoot is computed from internal state and is read-only
                //
                var autoRule = new DirectoryCopy
                {
                    // Empty Source => DirectoryCopy will treat extractDir as the source root
                    Source = string.Empty,

                    // Empty DestinationDirectory => copy directly into installDirectory
                    DestinationDirectory = string.Empty,

                    // We only want the contents, not an extra nested root folder
                    IncludeRootDirectory = false
                };

                autoRule.Execute(
                    instance.StockGame, // gamePath
                    extractDir,         // extractDir (root of extracted archive)
                    installDirectory,   // modDir (TOOLS/BSArch)
                    instance.InstallDir // instanceDir
                );

                // Single synthesized rule, so progress is trivially 1/1
                CopyingProgress(1, 1);
            }
            finally
            {
                // Always clean up the extracted cache directory
                ServiceSingleton.Files.RemoveDirectory(extractDir, true);
            }

            // Method is logically sync, but override is async
            await Task.CompletedTask;
        }
    }
}
