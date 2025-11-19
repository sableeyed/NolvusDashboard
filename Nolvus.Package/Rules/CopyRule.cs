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
            //Source = Encoding.UTF8.GetString(Encoding.GetEncoding("Windows-1252").GetBytes(Node["Source"].InnerText));
            Source = Node["Source"]?.InnerText ?? string.Empty;
            //Destination = System.Convert.ToInt16(Node["Destination"].InnerText);
            Destination = Convert.ToInt16(Node["Destination"]?.InnerText ?? "0");
            //DestinationDirectory = Node["DestinationDirectory"].InnerText;
            //DestinationDirectory = Node["DestinationDirectory"]?.InnerText ?? string.Empty;
            DestinationDirectory = NormalizeRulePath(Node["DestinationDirectory"]?.InnerText ?? string.Empty);
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
     
    }
}
