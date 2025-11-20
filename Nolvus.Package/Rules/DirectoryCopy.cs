using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Nolvus.Core.Services;
using Nolvus.Package.Utilities;

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

            // Determine base destination
            string destBase =
                Destination == 0 ? modDir :
                Destination == 1 ? gamePath :
                instanceDir;

            // ------------------------------------------------------------
            // 1) Try resolving Source path segment-by-segment
            //    Works for: "NetScriptFramework/Plugins", "_core/meshes", etc.
            // ------------------------------------------------------------
            string? resolvedSource = PathResolver.ResolvePathSegments(extractDir, Source);

            // ------------------------------------------------------------
            // 2) Fallback for FLAT archives (files directly at extract root)
            // ------------------------------------------------------------
            if (resolvedSource == null)
            {
                bool extractHasRootFiles = Directory.GetFiles(extractDir).Any();

                if (extractHasRootFiles)
                {
                    ServiceSingleton.Logger.Log(
                        $"DirectoryCopy fallback: using extract root '{extractDir}' because '{Source}' not found.");
                    resolvedSource = extractDir;
                }
            }

            // ------------------------------------------------------------
            // 3) If still null → path truly not found
            // ------------------------------------------------------------
            if (resolvedSource == null)
            {
                ServiceSingleton.Logger.Log($"DirectoryCopy skipped: source not found: {Source}");
                return;
            }

            string sourceAbsolute = resolvedSource;

            // ------------------------------------------------------------
            // 4) Determine final destination path
            // ------------------------------------------------------------
            string finalDestination =
                CopyToRoot
                    ? destBase
                    : Path.Combine(destBase, DestinationDirectory);

            Directory.CreateDirectory(finalDestination);

            // ------------------------------------------------------------
            // 5) Copy directories
            // ------------------------------------------------------------
            if (!IncludeRootDirectory)
            {
                // Only contents
                CopyDirectoryContents(sourceAbsolute, finalDestination);
            }
            else
            {
                // Include root folder name
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
