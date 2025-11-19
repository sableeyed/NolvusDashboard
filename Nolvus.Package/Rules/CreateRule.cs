using System;
using System.IO;
using System.Xml;

namespace Nolvus.Package.Rules
{
    public class CreateRule : Rule
    {
        public string Name { get; set; } = string.Empty;
        public int Source { get; set; } = 0;

        public override void Load(XmlNode Node)
        {
            base.Load(Node);

            // Normalize the name and source
            Name = NormalizePath(Node["Name"]?.InnerText ?? string.Empty);
            Source = Convert.ToInt32(Node["Source"]?.InnerText ?? "0");
        }

        public override void Execute(string GamePath, string ExtractDir, string ModDir, string InstanceDir)
        {
            throw new NotImplementedException("CreateRule does not implement Execute(). Derived classes must override.");
        }

        protected string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Replace Windows-style slashes
            path = path.Replace("\\", "/");

            // Trim leading "/" because these MUST be relative paths
            if (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
