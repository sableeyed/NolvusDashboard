using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Nolvus.Package.Rules
{
    public abstract class CopyRule : Rule
    {
        public string Source { get; set; } = string.Empty;
        public int Destination { get; set; } = 0;
        public string DestinationDirectory { get; set; } = string.Empty;
        public bool CopyToRoot
        {
            get { return this.DestinationDirectory == string.Empty; }
        }

        public override void Load(XmlNode Node)
        {
            base.Load(Node);
            Source = FixMangledNames(Node["Source"]?.InnerText ?? string.Empty);
            Destination = Convert.ToInt16(Node["Destination"]?.InnerText ?? "0");
            DestinationDirectory = NormalizeRulePath(FixMangledNames(Node["DestinationDirectory"]?.InnerText ?? string.Empty));
        }

        public override bool IsPriority
        {
            get
            {
                return true;
            }
        }   

        private string NormalizeRulePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Replace Windows slashes with Linux ones
            path = path.Replace("\\", "/");

            // If path starts with "/", remove it -> make relative
            if (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }

        private string FixMangledNames(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // detect UTF-8→1252 mojibake sequences ("æ", "ç", "è", "é", "å")
            if (!(input.Contains("æ") || input.Contains("ç") || input.Contains("è") || input.Contains("é") || input.Contains("å")))
                return input; // do nothing for valid Unicode names

            var bytes = Encoding.GetEncoding("Windows-1252").GetBytes(input);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
