using System;
using System.IO;
using System.Xml;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public class DeleteRule : Rule
    {
        public string Source { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public int Destination { get; set; } = 0;

        public override void Load(XmlNode Node)
        {
            base.Load(Node);

            Source = NormalizePath(Node["Source"]?.InnerText ?? string.Empty);
            IsDirectory = Convert.ToBoolean(Node["IsDirectory"]?.InnerText ?? "false");

            if (Node["Destination"] != null)
                Destination = Convert.ToInt16(Node["Destination"]?.InnerText ?? "0");
        }

        public override void Execute(string GamePath, string ExtractDir, string ModDir, string InstanceDir)
        {
            if (!CanExecute(GamePath, ModDir))
                return;

            string baseDir =
                Destination == 0 ? ModDir :
                Destination == 1 ? GamePath :
                InstanceDir;

            string fullPath = Path.Combine(baseDir, Source);

            // Nothing to delete
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                return;

            try
            {
                if (!IsDirectory)
                {
                    File.Delete(fullPath);
                }
                else
                {
                    ServiceSingleton.Files.RemoveDirectory(fullPath, true);
                }
            }
            catch (Exception ex)
            {
                // IMPORTANT: do not throw — deletion failure should NOT abort installs
                ServiceSingleton.Logger.Log($"DeleteRule failed on {fullPath}: {ex.Message}");
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Convert Windows slashes to Linux format
            path = path.Replace("\\", "/");

            // Remove leading slash (avoid deleting `/SKSE` etc)
            if (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
