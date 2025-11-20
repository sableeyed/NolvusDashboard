using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nolvus.Dashboard.Services.Wine
{
    public static class WineRunner
    {
        public static async Task<(int ExitCode, string Output, string Error)>
            RunAsync(string workingDirectory, string executable, params string[] arguments)
        {
            await WinePrefix.InitializeAsync();

            var psi = new ProcessStartInfo
            {
                FileName = executable,  // "wine", "winetricks", "protontricks", "bsarch.exe", etc.
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            psi.Environment["WINEPREFIX"] = WinePrefix.PrefixPath;

            foreach (var arg in arguments)
            {
                psi.ArgumentList.Add(arg);
            }

            var process = Process.Start(psi)!;

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return (process.ExitCode, stdout, stderr);
        }

        public static Task<(int ExitCode, string Output, string Error)>
            RunWineAsync(string workingDirectory, string exePath, params string[] args)
        {
            return RunAsync(workingDirectory, "wine", new[] { exePath }.Concat(args).ToArray());
        }

        public static Task<(int ExitCode, string Output, string Error)>
            WinetricksAsync(params string[] args)
        {
            return RunAsync(workingDirectory: "/", executable: "winetricks", args);
        }

        public static Task<(int ExitCode, string Output, string Error)>
            ProtontricksAsync(params string[] args)
        {
            return RunAsync(workingDirectory: "/", executable: "protontricks", args);
        }
    }
}
