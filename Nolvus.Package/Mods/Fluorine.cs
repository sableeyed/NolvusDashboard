using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Nolvus.Core.Services;

namespace Nolvus.Package.Mods
{
    public static class Fluorine
    {
        private const string ReleaseMarker = "nolvus-fluorine-release.txt";

        public static string InstallDirectory
        {
            get
            {
                var DataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

                if (string.IsNullOrWhiteSpace(DataHome))
                    DataHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

                return Path.Combine(DataHome, "fluorine", "bin");
            }
        }

        public static string Executable
        {
            get { return Path.Combine(InstallDirectory, "fluorine-manager"); }
        }

        internal static string MarkerFile
        {
            get { return Path.Combine(InstallDirectory, ReleaseMarker); }
        }

        public static bool IsInstalled
        {
            get { return File.Exists(Executable) || File.Exists(MarkerFile); }
        }

        // fluorine-manager is a bash wrapper that ends in `exec ModOrganizer-core`, so the wrapper
        // name is gone from the process table once it is up - match the core instead.
        public static bool IsRunning
        {
            get
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "pgrep",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add(Path.Combine(InstallDirectory, "ModOrganizer-core"));

                try
                {
                    using var p = Process.Start(psi);

                    if (p == null)
                        return false;

                    p.WaitForExit();

                    return p.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Native Linux binary, so no wine. The instance it opens comes from
        // ~/.config/Mod Organizer Team/Mod Organizer.conf, not from anything passed here.
        public static Process Start()
        {
            if (!IsInstalled)
                throw new FileNotFoundException("Fluorine Manager is not installed", Executable);

            var psi = new ProcessStartInfo
            {
                FileName = Executable,
                WorkingDirectory = InstallDirectory,
                UseShellExecute = false
            };

            ServiceSingleton.Logger.Log($"[FLUORINE] Launching {Executable}");

            return Process.Start(psi);
        }

        // Python plugins the Nolvus package drops into MO2/plugins that Fluorine should load too.
        // Fluorine only looks in its own bin/plugins, so these are brought across on every launch -
        // which also restores them after a Fluorine reinstall and picks up list updates.
        private static readonly string[] InstancePlugins = { "crdw_auto_mode" };

        /// <summary>
        /// Copies the instance's MO2 Python plugins listed in <see cref="InstancePlugins"/> into
        /// Fluorine's plugin folder. Bytecode caches are left behind, since they were compiled for
        /// MO2's Python and Fluorine builds its own. A plugin the instance does not have is skipped,
        /// and a failure is logged rather than stopping the launch.
        /// </summary>
        public static void InstallPlugins(string InstallDir)
        {
            foreach (var Plugin in InstancePlugins)
            {
                var Source = Path.Combine(InstallDir, "MO2", "plugins", Plugin);
                var Target = Path.Combine(InstallDirectory, "plugins", Plugin);

                if (!Directory.Exists(Source))
                    continue;

                try
                {
                    foreach (var File_ in Directory.EnumerateFiles(Source, "*", SearchOption.AllDirectories))
                    {
                        var Relative = Path.GetRelativePath(Source, File_);

                        if (Relative.Split(Path.DirectorySeparatorChar).Contains("__pycache__"))
                            continue;

                        var Destination = Path.Combine(Target, Relative);

                        Directory.CreateDirectory(Path.GetDirectoryName(Destination));
                        File.Copy(File_, Destination, true);
                    }

                    ServiceSingleton.Logger.Log($"[FLUORINE] Plugin {Plugin} copied from {Source} to {Target}");
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log($"[FLUORINE] Could not copy plugin {Plugin} to {Target} : {ex.Message}");
                }
            }
        }
    }
}
