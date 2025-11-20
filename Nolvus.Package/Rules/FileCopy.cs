using System;
using System.IO;
using System.Xml;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public class FileCopy : CopyRule
    {
        public string NewFileName { get; set; }

        public override void Load(XmlNode node)
        {
            base.Load(node);

            // Normalize all inherited paths
            Source = Normalize(Source);
            DestinationDirectory = Normalize(DestinationDirectory);

            NewFileName = Normalize(node["NewFileName"]?.InnerText ?? string.Empty);
        }

        public override void Execute(string gamePath, string extractDir, string modDir, string instanceDir)
        {
            if (!CanExecute(gamePath, modDir))
                return;

            ServiceSingleton.Logger.Log(
                $"[FileCopy] Source='{Source}' DestDir='{DestinationDirectory}' NormalizedSource='{Normalize(Source)}' NormalizedDest='{Normalize(DestinationDirectory)}'"
            );

            // Always normalize before using
            string srcRel = Normalize(Source);
            string destRel = Normalize(DestinationDirectory);

            // Convert to platform separators
            string destRelPlatform =
                destRel.Replace("/", Path.DirectorySeparatorChar.ToString());

            // Pick destination base
            string destBase =
                Destination == 0 ? modDir :
                Destination == 1 ? gamePath :
                instanceDir;

            // Resolve full source path
            // Convert to platform separators:
            string srcPlatform = srcRel.Replace("/", Path.DirectorySeparatorChar.ToString());
            string sourceFull = Path.Combine(extractDir, srcPlatform);

            if (!File.Exists(sourceFull))
            {
                ServiceSingleton.Logger.Log($"[FileCopy] Skipping missing file: {sourceFull}");
                return;
            }

            // Determine actual directory to copy into
            string targetDir = CopyToRoot
                ? destBase
                : Path.Combine(destBase, destRelPlatform);

            Directory.CreateDirectory(targetDir);

            // Determine final filename
            string finalName = !string.IsNullOrWhiteSpace(NewFileName)
                ? Normalize(NewFileName)
                : Path.GetFileName(sourceFull);

            finalName = finalName.Replace("/", Path.DirectorySeparatorChar.ToString());

            string destFull = Path.Combine(targetDir, finalName);

            // Perform the copy
            File.Copy(sourceFull, destFull, overwrite: true);

            ServiceSingleton.Logger.Log($"[FileCopy] Copied: {sourceFull} → {destFull}");
        }

        private string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Windows → Unix separators first
            path = path.Replace("\\", "/");

            // Prevent absolute paths
            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
