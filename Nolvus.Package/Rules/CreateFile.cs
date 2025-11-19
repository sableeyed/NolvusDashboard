using System;
using System.IO;
using System.Xml;

namespace Nolvus.Package.Rules
{
    public class CreateFile : CreateRule
    {
        public string DefaultText { get; set; } = string.Empty;

        public override void Load(XmlNode Node)
        {
            base.Load(Node);
            DefaultText = Node["DefaultText"]?.InnerText ?? string.Empty;
        }

        public override void Execute(string GamePath, string ExtractDir, string ModDir, string InstanceDir)
        {
            if (!CanExecute(GamePath, ModDir))
                return;

            // Determine base directory
            string baseDir = (Source == 0) ? ModDir : GamePath;

            // Normalize path relative to base directory
            string fullPath = Path.Combine(baseDir, Name);

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            if (!string.IsNullOrEmpty(DefaultText))
            {
                File.WriteAllText(fullPath, DefaultText);
            }
            else
            {
                File.Create(fullPath).Dispose();
            }
        }
    }
}
