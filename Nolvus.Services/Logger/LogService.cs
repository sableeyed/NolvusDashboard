using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Services.Logger
{
    public class LogService : ILogService
    {
        private readonly object _sync = new();
        private static string Filename = string.Empty;

        public void Log(string message)
        {
            var line = $"{DateTime.Now:HH:mm:ss} {message}";

            lock (_sync)
            {
                Console.WriteLine(line);
                File.AppendAllText(Path.Combine(ServiceSingleton.Folders.LogsDirectory, Filename), line + Environment.NewLine);
            }
        }

        public void LineBreak()
        {
            Log("");
        }

        public void InitializeLog()
        {
            Filename = string.Format("Log-{0}.txt", DateTime.Now.ToString("yyyydd-M-HHmmss"));
        }

        public void ClearLogs()
        {
            var LogsToDelete = ServiceSingleton.Files.GetFiles(ServiceSingleton.Folders.LogsDirectory).Where(x => x.Name != Filename).ToList();

            foreach (var LogFile in LogsToDelete)
            {
                LogFile.Delete();
            }
        }
    }
}
