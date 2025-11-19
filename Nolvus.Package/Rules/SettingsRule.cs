using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public class SettingsRule : Rule
    {
        public string FileName { get; set; }
        public bool IsIni { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Section { get; set; }

        public override void Load(XmlNode node)
        {
            base.Load(node);

            FileName = Normalize(node["FileName"]?.InnerText);
            IsIni = Convert.ToBoolean(node["IsIni"]?.InnerText ?? "false");
            Key = node["Key"]?.InnerText ?? string.Empty;
            Value = node["Value"]?.InnerText ?? string.Empty;
            Section = node["Section"]?.InnerText ?? string.Empty;
        }

        public override void Execute(string gamePath, string extractDir, string modDir, string instanceDir)
        {
            if (!CanExecute(gamePath, modDir))
                return;

            string fullPath = Path.Combine(modDir, FileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

            if (IsIni)
            {
                ServiceSingleton.Settings.StoreIniValue(fullPath, Section, Key, Value);
                return;
            }

            // Non-INI plaintext config
            if (!File.Exists(fullPath))
            {
                ServiceSingleton.Logger.Log($"SettingsRule: file not found: {fullPath}");
                return;
            }

            string[] lines = File.ReadAllLines(fullPath);
            List<string> newLines = new List<string>(lines.Length);

            bool replaced = false;

            foreach (string line in lines)
            {
                string newLine = line;

                // Skip comments
                if (!line.TrimStart().StartsWith("#") && !line.TrimStart().StartsWith(";"))
                {
                    // Match "Key = value" type entries
                    string trimmed = line.Trim();

                    if (!replaced &&
                        trimmed.StartsWith(Key) &&
                        (trimmed.Contains("=") || trimmed.Contains(" ")))
                    {
                        newLine = $"{Key} = {Value}";
                        replaced = true;
                    }
                }

                newLines.Add(newLine);
            }

            // If key never existed, append it
            if (!replaced)
                newLines.Add($"{Key} = {Value}");

            File.WriteAllLines(fullPath, newLines);
        }

        private string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Convert windows slashes
            path = path.Replace("\\", "/");

            // Never allow absolute path
            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
