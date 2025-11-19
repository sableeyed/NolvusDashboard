using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public class EnvironmentSettingsRule : SettingsRule
    {
        public override void Execute(string gamePath, string extractDir, string modDir, string instanceDir)
        {
            if (!CanExecute(gamePath, modDir))
                return;

            string envValue = ServiceSingleton.Instances.GetValueFromKey(Value);

            // Normalize file name
            string normalizedFile = NormalizePath(FileName);

            // Use Path.Combine (cross-platform)
            string settingsFile = Path.Combine(modDir, normalizedFile);

            if (!File.Exists(settingsFile))
            {
                ServiceSingleton.Logger.Log($"EnvironmentSettingsRule skipped: file not found: {settingsFile}");
                return;
            }

            if (IsIni)
            {
                ServiceSingleton.Settings.StoreIniValue(settingsFile, Section, Key, envValue);
                return;
            }

            // Non-INI plaintext settings
            var lines = File.ReadAllLines(settingsFile).ToList();
            bool replaced = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();

                // Skip comments (#, //, ;)
                if (line.StartsWith("#") || line.StartsWith("//") || line.StartsWith(";"))
                    continue;

                // Match exact key at start of line (case-insensitive)
                if (line.StartsWith(Key + " ", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith(Key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = $"{Key} = {envValue}";
                    replaced = true;
                    break;
                }
            }

            // If key did not exist → append
            if (!replaced)
                lines.Add($"{Key} = {envValue}");

            File.WriteAllLines(settingsFile, lines);
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Replace Windows slashes
            path = path.Replace("\\", "/");

            // Remove leading Linux slash (avoid absolute path)
            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
