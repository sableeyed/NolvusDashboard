using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Services;
using Nolvus.Core.Utils;

namespace Nolvus.Dashboard.Services
{
    public static class WinePrefix
    {
        public static string PrefixPath { get; }

        static WinePrefix()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            PrefixPath = Path.Combine(local, "NolvusDashboard", "prefix");
        }

        public static bool Exists => Directory.Exists(PrefixPath);

        public static string ToWinePath(string linuxPath)
        {
            if (string.IsNullOrWhiteSpace(linuxPath))
                throw new ArgumentNullException(nameof(linuxPath));

            return "Z:" + linuxPath.Replace("/", "\\");
        }

        public static async Task InitializeAsync(Action<string, double>? progress = null)
        {
            // If the prefix directory exists, assume it's valid.
            if (Exists)
            {
                progress?.Invoke("Prefix exists, skipping...", 100);
                ServiceSingleton.Logger.Log($"Wine prefix already exists: {PrefixPath}, skipping initialization");
                return;
            }

            progress?.Invoke("Preparing prefix…", 10);
            Directory.CreateDirectory(PrefixPath);

            // Initialize prefix once
            var psi = new ProcessStartInfo
            {
                FileName = "wineboot",
                Arguments = "--init",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            psi.Environment["WINEPREFIX"] = PrefixPath;

            var proc = Process.Start(psi)!;
            await proc.WaitForExitAsync();

            var steps = new[]
            {
                ("Installing C++ Runtime", "--force vcrun2022"),
                ("Installing .NET Framework", "dotnet48 -q"),
                ("Installing Fonts", "corefonts -q "),
                ("Setting Font Smoothing", "fontsmooth=rgb -q"),
            };

            int index = 1;
            foreach (var (label, winetrick) in steps)
            {
                double pct = 10 + index * (80.0 / steps.Length); // fills 10% → 90%
                progress?.Invoke(label, pct);
                await RunWinetricksAsync(winetrick);
                index++;
            }

            progress?.Invoke("Finalizing…", 95);
            await Task.Delay(300);

            progress?.Invoke("Ready!", 100);
        }

        public static async Task<int> RunWinetricksAsync(params string[] args)
        {
            if (string.IsNullOrWhiteSpace(PrefixPath))
                throw new InvalidOperationException("Wine Prefix was not initialized");
            
            string winetricks = PathResolver.FindExecutable("winetricks")
                ?? throw new Exception("winetricks not found in PATH");

            var startInfo = new ProcessStartInfo
            {
                FileName = winetricks,
                ArgumentList = { },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment =
                {
                    ["WINEPREFIX"] = PrefixPath,
                    ["WINE"] = PathResolver.FindExecutable("wine") ?? "wine",
                    ["WINECFG"] = PathResolver.FindExecutable("winecfg") ?? "winecfg",
                }
            };

            foreach (var arg in args)
                foreach (var token in arg.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    startInfo.ArgumentList.Add(token);

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<int>();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    ServiceSingleton.Logger.Log("[WINETRICKS] " + e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    ServiceSingleton.Logger.Log("[WINETRICKS-ERR] " + e.Data);
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
    }
}
