using System;
using System.IO;
using System.Xml;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public class OptionalEsp : Rule
    {
        public string EspName { get; set; }

        public override void Load(XmlNode node)
        {
            base.Load(node);

            EspName = Normalize(node["EspName"]?.InnerText ?? string.Empty);
        }

        public override void Execute(string gamePath, string extractDir, string modDir, string instanceDir)
        {
            if (!CanExecute(gamePath, modDir))
                return;

            string optionalDir = Path.Combine(modDir, "optional");
            Directory.CreateDirectory(optionalDir);

            string sourcePath = Path.Combine(modDir, EspName);
            string destPath = Path.Combine(optionalDir, EspName);

            if (!File.Exists(sourcePath))
            {
                ServiceSingleton.Logger.Log($"OptionalEsp: Source ESP not found: {sourcePath}");
                return;
            }

            // Ensure destination folder exists
            File.Copy(sourcePath, destPath, overwrite: true);

            try
            {
                File.Delete(sourcePath);
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"OptionalEsp: Failed to delete source file {sourcePath}: {ex.Message}");
            }
        }

        private string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Convert Windows → Linux separators
            path = path.Replace("\\", "/");

            // Prevent absolute paths (/something)
            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
