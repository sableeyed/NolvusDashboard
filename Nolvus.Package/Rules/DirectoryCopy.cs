using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public class DirectoryCopy : CopyRule
    {
        public bool IncludeRootDirectory { get; set; }

        public override void Load(XmlNode node)
        {
            base.Load(node);
            IncludeRootDirectory = Convert.ToBoolean(node["IncludeRootDirectory"]?.InnerText ?? "false");

            // Normalize inherited CopyRule.Source and CopyRule.DestinationDirectory
            Source = NormalizePath(Source);
            DestinationDirectory = NormalizePath(DestinationDirectory);
        }

        public override void Execute(string gamePath, string extractDir, string modDir, string instanceDir)
        {
            if (!CanExecute(gamePath, modDir))
                return;

            string destBase =
                Destination == 0 ? modDir :
                Destination == 1 ? gamePath :
                instanceDir;

            string sourceAbsolute = Path.Combine(extractDir, Source);
            if (!Directory.Exists(sourceAbsolute))
            {
                ServiceSingleton.Logger.Log($"DirectoryCopy skipped: source not found: {sourceAbsolute}");
                return;
            }

            // If no subdirectory override, copy into root of destination
            string finalDestination = destBase;

            if (!CopyToRoot)
            {
                finalDestination = Path.Combine(destBase, DestinationDirectory);
            }

            Directory.CreateDirectory(finalDestination);

            if (!IncludeRootDirectory)
            {
                // Copy CONTENTS of the directory
                CopyDirectoryContents(sourceAbsolute, finalDestination);
            }
            else
            {
                // Copy directory including the root folder
                string rootName = new DirectoryInfo(sourceAbsolute).Name;
                string destWithRoot = Path.Combine(finalDestination, rootName);
                Directory.CreateDirectory(destWithRoot);

                CopyDirectoryContents(sourceAbsolute, destWithRoot);
            }
        }

        private void CopyDirectoryContents(string src, string dst)
        {
            // Copy files
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                string localPath = Path.GetRelativePath(src, file);
                localPath = NormalizePath(localPath);

                string targetPath = Path.Combine(dst, localPath);

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(file, targetPath, true);
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            path = path.Replace("\\", "/");

            // Prevent absolute paths on Linux
            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
