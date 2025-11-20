using System;
using System.Xml;
using System.IO;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public class XMLSettingsRule : Rule
    {
        public string FileName { get; set; }
        public string Key { get; set; }
        public bool Variable { get; set; }
        public string Value { get; set; }

        public override void Load(XmlNode Node)
        {
            base.Load(Node);

            FileName = Normalize(Node["FileName"]?.InnerText ?? "");
            Key      = Node["Key"]?.InnerText ?? "";
            Variable = Convert.ToBoolean(Node["Variable"]?.InnerText ?? "false");
            Value    = Node["Value"]?.InnerText ?? "";
        }

        public override void Execute(string GamePath, string ExtractDir, string ModDir, string InstanceDir)
        {
            if (!CanExecute(GamePath, ModDir))
                return;

            string envValue = Variable
                ? ServiceSingleton.Instances.GetValueFromKey(Value)
                : Value;

            // Normalize path again at runtime (safety)
            string normalizedFile = Normalize(FileName);

            // Build actual file path
            string fullPath = Path.Combine(ModDir, normalizedFile);

            if (!File.Exists(fullPath))
            {
                ServiceSingleton.Logger.Log(
                    $"[XMLSettingsRule] File not found: {fullPath}");
                return;
            }

            XmlDocument xml = new XmlDocument();
            xml.Load(fullPath);

            XmlNode keyNode = xml.SelectSingleNode(Key);

            if (keyNode == null)
            {
                ServiceSingleton.Logger.Log(
                    $"[XMLSettingsRule] XPath '{Key}' not found in: {fullPath}");
                return;
            }

            keyNode.InnerText = envValue;

            xml.Save(fullPath);

            ServiceSingleton.Logger.Log(
                $"[XMLSettingsRule] Updated '{Key}' in {fullPath}");
        }

        private string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Convert Windows separators → Linux
            path = path.Replace("\\", "/");

            // Remove accidental leading slashes to avoid absolute paths
            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
