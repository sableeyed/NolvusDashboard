using System;
using System.IO;
using System.Xml;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public class RenameRule : Rule
    {
        public string OldName { get; set; }
        public string NewName { get; set; }
        public int Source { get; set; }
        public bool IsDirectory { get; set; }

        public override void Load(XmlNode node)
        {
            base.Load(node);

            OldName = Normalize(node["OldName"]?.InnerText ?? string.Empty);
            NewName = Normalize(node["NewName"]?.InnerText ?? string.Empty);
            Source = Convert.ToInt32(node["Source"]?.InnerText ?? "0");
            IsDirectory = Convert.ToBoolean(node["IsDirectory"]?.InnerText ?? "false");
        }

        public override void Execute(string gamePath, string extractDir, string modDir, string instanceDir)
        {
            if (!CanExecute(gamePath, modDir))
                return;

            string baseDir = (Source == 0) ? modDir : gamePath;

            string srcPath = Path.Combine(baseDir, OldName);
            string dstPath = Path.Combine(baseDir, NewName);

            // Ensure containing directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);

            if (!IsDirectory)
            {
                if (!File.Exists(srcPath))
                {
                    ServiceSingleton.Logger.Log($"RenameRule: Source file missing: {srcPath}");
                    return;
                }

                // Delete existing file if needed
                if (File.Exists(dstPath))
                {
                    try { File.Delete(dstPath); }
                    catch (Exception ex)
                    {
                        ServiceSingleton.Logger.Log($"RenameRule: Failed to delete existing file {dstPath}: {ex.Message}");
                        return;
                    }
                }

                try
                {
                    File.Move(srcPath, dstPath);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log($"RenameRule: Failed to rename {srcPath} → {dstPath}: {ex.Message}");
                }
            }
            else
            {
                if (!Directory.Exists(srcPath))
                {
                    ServiceSingleton.Logger.Log($"RenameRule: Source directory missing: {srcPath}");
                    return;
                }

                // Delete or move depending on existence
                if (Directory.Exists(dstPath))
                {
                    try
                    {
                        Directory.Delete(dstPath, true);
                    }
                    catch (Exception ex)
                    {
                        ServiceSingleton.Logger.Log($"RenameRule: Failed to remove existing directory {dstPath}: {ex.Message}");
                        return;
                    }
                }

                try
                {
                    Directory.Move(srcPath, dstPath);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log($"RenameRule: Failed to rename directory {srcPath} → {dstPath}: {ex.Message}");
                }
            }
        }

        private string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            path = path.Replace("\\", "/");

            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
