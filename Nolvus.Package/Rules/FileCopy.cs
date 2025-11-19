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

            NewFileName = Normalize(node["NewFileName"]?.InnerText ?? string.Empty);

            // Normalize inherited values from CopyRule
            Source = Normalize(Source);
            DestinationDirectory = Normalize(DestinationDirectory);
        }

        public override void Execute(string gamePath, string extractDir, string modDir, string instanceDir)
        {
            if (!CanExecute(gamePath, modDir))
                return;

            // Normalize paths at runtime (extra safety)
            var sourceRel = Normalize(Source);
            var destDirRel = Normalize(DestinationDirectory);

            // Select actual root destination
            string rootDest =
                Destination == 0 ? modDir :
                Destination == 1 ? gamePath :
                instanceDir;

            string sourceFull = Path.Combine(extractDir, sourceRel);

            if (!File.Exists(sourceFull))
            {
                ServiceSingleton.Logger.Log($"FileCopy skipping: missing source file {sourceFull}");
                return;
            }

            // Determine final destination directory
            string targetDirectory = rootDest;

            if (!CopyToRoot)
            {
                targetDirectory = Path.Combine(rootDest, destDirRel);
                Directory.CreateDirectory(targetDirectory);
            }

            // Determine final file name
            string destFileName = !string.IsNullOrWhiteSpace(NewFileName)
                ? Normalize(NewFileName)
                : Path.GetFileName(sourceFull);

            string destFull = Path.Combine(targetDirectory, destFileName);

            // Perform the copy
            File.Copy(sourceFull, destFull, overwrite: true);

            ServiceSingleton.Logger.Log($"Copied file: {sourceFull} → {destFull}");
        }

        private string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Convert Windows separators → Linux
            path = path.Replace("\\", "/");

            // Remove leading "/" so path isn't absolute
            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
