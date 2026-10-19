using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Tests
{
    /// <summary>
    /// Stands in for the parts of the dashboard's startup the code under test relies on.
    /// </summary>
    internal static class TestEnvironment
    {
        public static RecordingLogger Logger { get; } = new RecordingLogger();

        [ModuleInitializer]
        internal static void Initialize()
        {
            // Program.cs registers this at startup; without it Windows-1252 is not available and
            // CopyRule's name repair throws on any non-ASCII path.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ServiceSingleton.RegisterService<ILogService>(Logger);

            // Fluorine.IsRunning looks for ModOrganizer-core under $XDG_DATA_HOME/fluorine/bin.
            // Pointing it at an empty folder keeps a Fluorine open on this machine from making
            // the ini tests skip their work.
            var DataHome = Path.Combine(Path.GetTempPath(), "nolvus-tests-xdg-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DataHome);
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", DataHome);

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try { Directory.Delete(DataHome, true); } catch { }
            };
        }
    }

    internal sealed class RecordingLogger : ILogService
    {
        public ConcurrentQueue<string> Messages { get; } = new ConcurrentQueue<string>();

        public void Log(string Message) => Messages.Enqueue(Message);

        public void LineBreak() { }

        public void ClearLog() => Messages.Clear();
    }

    /// <summary>
    /// A scratch folder under /tmp, removed when the test ends.
    /// </summary>
    internal sealed class TempDir : IDisposable
    {
        public string Path { get; }

        public TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nolvus-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        /// <summary>Creates a file (and its folders) at a path relative to the root.</summary>
        public string File(string RelativePath, string Content = "")
        {
            var Full = System.IO.Path.Combine(Path, RelativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Full)!);
            System.IO.File.WriteAllText(Full, Content);
            return Full;
        }

        /// <summary>Creates a folder at a path relative to the root.</summary>
        public string Dir(string RelativePath)
        {
            var Full = System.IO.Path.Combine(Path, RelativePath);
            Directory.CreateDirectory(Full);
            return Full;
        }

        public string Combine(params string[] Parts) => System.IO.Path.Combine(new[] { Path }.Concat(Parts).ToArray());

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }

    internal static class Xml
    {
        /// <summary>Parses a manifest fragment such as &lt;Rule&gt;...&lt;/Rule&gt; into its root element.</summary>
        public static System.Xml.XmlElement Parse(string Fragment)
        {
            var Doc = new System.Xml.XmlDocument();
            Doc.LoadXml(Fragment);
            return Doc.DocumentElement!;
        }
    }
}
