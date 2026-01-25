using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Services;
using Nolvus.Core.Utils;
using Nolvus.Core.Interfaces;

namespace Nolvus.Dashboard.Services.Proton
{
    public class Protontricks : IProtontricks
    {
        public Task<string?> GetPrefixPathAsync(string appId)
        {
            if (string.IsNullOrWhiteSpace(appId)) 
            {
                ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", "appId was NULL, please report this as a bug");
                return Task.FromResult<string?>(null);
            }
                //throw new ArgumentNullException(nameof(appId));

            string? vdf = FindLibraryFoldersVdf();
            if (vdf == null)
                return Task.FromResult<string?>(null);

            var libraries = GetSteamLibraries(vdf);

            // Also include the primary Steam root itself (vdf lives under <steamroot>/steamapps/)
            // This covers setups where compatdata lands in the primary root even if libraries are funky.
            var steamRoot = Directory.GetParent(Path.GetDirectoryName(vdf)!)?.FullName;
            if (!string.IsNullOrWhiteSpace(steamRoot))
                libraries.Insert(0, steamRoot);

            foreach (var lib in libraries.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var pfx = Path.Combine(lib, "steamapps", "compatdata", appId, "pfx");

                ServiceSingleton.Logger.Log($"[PROTONTRICKS] Checking prefix: {pfx}");

                if (Directory.Exists(pfx))
                    return Task.FromResult<string?>(pfx);
            }

            return Task.FromResult<string?>(null);
        }



        public Task<int> RunAsync(string appId, params string[] args)
        {
            if (string.IsNullOrWhiteSpace(appId))
            {
                ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", "appId was NULL, please report this as a bug");
                return Task.FromResult(-1);
            }

            string protontricks = ExecutableResolver.FindExecutable("protontricks");
            
            if (string.IsNullOrWhiteSpace(protontricks)) 
            {
                ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", "Protontricks was not found in $PATH");
                return Task.FromResult(-1);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = protontricks,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.Environment["PROTONTRICKS_NO_GUI"] = "1";

            startInfo.ArgumentList.Add(appId);

            foreach (var arg in args)
            {
                foreach (var token in arg.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    startInfo.ArgumentList.Add(token);
            }

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    ServiceSingleton.Logger.Log("[PROTONTRICKS] " + e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    ServiceSingleton.Logger.Log("[PROTONTRICKS-ERR] " + e.Data);
            };

            process.Exited += (_, __) =>
            {
                tcs.TrySetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }

        public async Task ConfigureAsync(string appId, string instanceInstallDir, Action<string, double>? progress = null)
        {
            if (string.IsNullOrWhiteSpace(appId)) 
            {
                await ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", "appId was NULL, please report this as a bug");
                return;
            }

            if (string.IsNullOrWhiteSpace(instanceInstallDir))
            {
                await ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", "Could not find Nolvus install path");
                return;
            }

            if (!Directory.Exists(instanceInstallDir)) 
            {
                await ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", "Could not find Nolvus install path");
                return;
            }

            //progress?.Invoke("Locating Proton prefix…", 5);
            ServiceSingleton.Dashboard.Progress(5);

            string? prefix = await GetPrefixPathAsync(appId);
            if (prefix == null) 
            {
                await ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", "Could not find Skyrim data paths, have you ran it once?");
                return;
            }

            // 2) install required verbs
            //progress?.Invoke("Installing prerequisites…", 20);
            ServiceSingleton.Dashboard.Progress(20);

            int installExit = await RunAsync(appId,
                "-q",
                "vcrun2022",
                "dotnet48",
                "dxvk",
                "corefonts",
                "d3dcompiler_42",
                "d3dcompiler_43",
                "d3dcompiler_46",
                "d3dcompiler_47");

            if (installExit != 0) 
            {
                await ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", $"Protontricks returned error code {installExit}");
                return;
            }

            // 3) reset windows version
            //progress?.Invoke("Setting Windows version…", 65);
            ServiceSingleton.Dashboard.Progress(65);

            int winverExit = await RunAsync(appId, "-q", "win10");
            if (winverExit != 0) 
            {
                await ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", $"Protontricks failed to change windows version {winverExit}");
                return;
            }

            // 5) symlink X: -> instanceInstallDir
            //progress?.Invoke("Mapping X: drive…", 75);
            ServiceSingleton.Dashboard.Progress(75);

            string dosdevices = Path.Combine(prefix, "dosdevices");
            Directory.CreateDirectory(dosdevices);

            string xDrive = Path.Combine(dosdevices, "x:");
            TryDeleteFileOrDir(xDrive);

            File.CreateSymbolicLink(xDrive, instanceInstallDir);

            // 4) copy d3dcompiler_47.dll to STOCK GAME
            //progress?.Invoke("Copying d3dcompiler_47.dll…", 85);
            ServiceSingleton.Dashboard.Progress(85);

            string stockGame = Path.Combine(instanceInstallDir, "STOCK GAME");
            Directory.CreateDirectory(stockGame);

            string driveC = Path.Combine(prefix, "drive_c");
            string[] candidates =
            {
                Path.Combine(driveC, "windows", "system32", "d3dcompiler_47.dll"),
                Path.Combine(driveC, "windows", "syswow64", "d3dcompiler_47.dll"),
            };

            bool copied = false;
            foreach (var src in candidates)
            {
                if (!File.Exists(src))
                    continue;

                string dst = Path.Combine(stockGame, "d3dcompiler_47.dll");
                File.Copy(src, dst, overwrite: true);
                copied = true;
                break; // one is enough
            }

            if (!copied) 
            {
                await ServiceSingleton.Dashboard.Error("Prefix Configuration Failed", $"Unable to find d3dcompiler_47.dll, please place a copy manually in STOCK GAME. Otherwise everything else succeeded");
                return;
            }

            //progress?.Invoke("Ready!", 100);
            ServiceSingleton.Dashboard.Progress(100);
        }

        public static List<string> GetSteamLibraries(string vdfPath)
        {
            var libs = new List<string>();

            if (!File.Exists(vdfPath))
                return libs;

            string[] lines = File.ReadAllLines(vdfPath);

            bool insideLibrary = false;

            foreach (var raw in lines)
            {
                string line = raw.Trim();

                // Start of a library entry: "0", "1", "2", ...
                if (line.StartsWith("\"") && line.EndsWith("\"") && line.Length <= 4)
                {
                    insideLibrary = true;
                    continue;
                }

                if (!insideLibrary)
                    continue;

                if (line.StartsWith("\"path\""))
                {
                    int idx = line.IndexOf('"', 7);
                    if (idx > 0)
                    {
                        int end = line.IndexOf('"', idx + 1);
                        if (end > idx)
                        {
                            var path = line.Substring(idx + 1, end - (idx + 1));
                            if (!string.IsNullOrWhiteSpace(path))
                                libs.Add(path);
                        }
                    }
                    continue;
                }

                if (line == "}")
                {
                    // Leaving a library block
                    insideLibrary = false;
                }
            }

            return libs;
        }


        private static string? FindLibraryFoldersVdf()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Most common locations for libraryfolders.vdf
            string[] candidates =
            {
                Path.Combine(home, ".steam", "steam", "steamapps", "libraryfolders.vdf"),
                Path.Combine(home, ".local", "share", "Steam", "steamapps", "libraryfolders.vdf"),

                // Flatpak Steam
                Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", "data", "Steam", "steamapps", "libraryfolders.vdf"),
            };

            foreach (var c in candidates)
            {
                if (File.Exists(c))
                    return c;
            }

            return null;
        }


        private static void TryDeleteFileOrDir(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return;
                }

                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch {}
        }
    }
}
