using System.Diagnostics;
using Nolvus.Core.Services;

namespace Nolvus.Dashboard.Services.Wine;

public static class WineBsarch
{
    public static async Task<int> UnpackAsync(string bsarchExe, string archive, string outputDir)
    {
        string winBsarch = bsarchExe.Replace("\\", "/");
        string winArchive = archive.Replace("\\", "/");
        string winOutput = outputDir.Replace("\\", "/");

        var psi = new ProcessStartInfo
        {
            FileName = "/usr/bin/wine",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add(winBsarch);
        psi.ArgumentList.Add("unpack");
        psi.ArgumentList.Add(winArchive);
        psi.ArgumentList.Add(winOutput);

        psi.Environment["WINEPREFIX"] = WinePrefix.PrefixPath;
        psi.Environment["WINEDEBUG"] = "-all";

        ServiceSingleton.Logger.Log($"[BSARCH-WINE] unpack {archive}");

        var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var tcs = new TaskCompletionSource<int>();

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
                ServiceSingleton.Logger.Log("[BSARCH] " + e.Data);
        };

        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
                ServiceSingleton.Logger.Log("[BSARCH-ERR] " + e.Data);
        };

        p.Exited += (_, __) =>
        {
            tcs.TrySetResult(p.ExitCode);
            p.Dispose();
        };

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        return await tcs.Task;
    }
}
