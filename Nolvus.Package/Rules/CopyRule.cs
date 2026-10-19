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
            // Some package paths are UTF-8 that was read as Windows-1252 somewhere upstream (日本語
            // arrives as æ—¥æœ¬èªž, — as â€”). Undo that only when it provably happened: the text
            // must encode to 1252 exactly and those bytes must be valid UTF-8. Real CJK cannot be
            // encoded to 1252, and a real é is not valid UTF-8 on its own, so both are left alone.
            if (string.IsNullOrEmpty(input) || input.All(c => c < 128))
                return input;

            byte[] bytes;

            try
            {
                bytes = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(input);
            }
            catch (EncoderFallbackException)
            {
                return input;
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return input;
            }
        }
    }
}
