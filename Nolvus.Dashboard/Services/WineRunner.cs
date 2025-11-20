using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Dashboard.Services.Wine
{
    public class WineRunner : IWineRunner
    {
        public async Task<int> RunAsync(string workingDirectory, string exeName, params string[] args)
        {
            await WinePrefix.InitializeAsync();

            if (string.IsNullOrWhiteSpace(WinePrefix.PrefixPath))
                throw new InvalidOperationException("Wine prefix path not set.");

            if (!Directory.Exists(workingDirectory))
                throw new DirectoryNotFoundException($"Working directory not found: {workingDirectory}");

            var psi = new ProcessStartInfo
            {
                FileName = "wine",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // First argument: the exe we run under wine
            psi.ArgumentList.Add(exeName);

            // Next arguments: EXACTLY AS PROVIDED
            foreach (var a in args)
                psi.ArgumentList.Add(a);

            psi.Environment["WINEPREFIX"] = WinePrefix.PrefixPath;
            psi.Environment["WINEDEBUG"] = "-all";

            ServiceSingleton.Logger.Log("[WINE] Running: wine " + exeName + " " + string.Join(" ", args));

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var tcs = new TaskCompletionSource<int>();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    ServiceSingleton.Logger.Log($"[WINE] {e.Data}");
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    ServiceSingleton.Logger.Log($"[WINE-ERR] {e.Data}");
            };

            process.Exited += (_, __) =>
            {
                tcs.TrySetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return await tcs.Task;
        }

        public string ToWinePath(string linuxPath)
        {
            linuxPath = linuxPath.TrimEnd('/', '\\'); // prevent \Z:\ bug
            return WinePrefix.ToWinePath(linuxPath);
        }
    }
}
