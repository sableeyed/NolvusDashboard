using System.IO;
using System.Reflection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Nolvus.Package.Properties
{
    internal static class Resources
    {
        private static readonly Assembly asm = typeof(Resources).Assembly;

        private static Stream GetResource(string name)
        {
            // Nolvus.Package.Resources.filename
            var full = $"Nolvus.Package.Resources.{name}";
            var stream = asm.GetManifestResourceStream(full);
            if (stream == null)
                throw new FileNotFoundException($"Resource not found: {full}");
            return stream;
        }

        internal static Image mod_def_22
            => Image.Load(GetResource("mod-def-22.png"));

        internal static Image splash_v5
            => Image.Load(GetResource("splash_v5.png"));

        internal static Image splash_v6
            => Image.Load(GetResource("splash_v6.png"));
    }
}
