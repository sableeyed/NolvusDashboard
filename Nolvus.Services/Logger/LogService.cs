using System;
using System.IO;
using Nolvus.Core.Interfaces;

namespace Nolvus.Services.Logger
{
    public class LogService : ILogService
    {
        private readonly object _sync = new();
        private readonly string _logFilePath;

        public LogService()
        {
            var baseDir = AppContext.BaseDirectory;
            var logDir = Path.Combine(baseDir, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // Daily log file rotate
            _logFilePath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
        }

        public void Log(string message)
        {
            var line = $"{DateTime.Now:HH:mm:ss} {message}";

            lock (_sync)
            {
                Console.WriteLine(line);
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
        }

        public void LineBreak()
        {
            Log("");
        }
    }
}
