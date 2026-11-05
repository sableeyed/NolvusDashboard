using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Misc;

namespace Nolvus.Core.Services
{
    public class ReportService : IReportService
    {
        // Colors / constants
        private static readonly string BgHex = "#363636";
        private static readonly string OrangeHex = "#FFA500";

        private static readonly string[] ReportFonts =
            { "Open Sans", "Noto Sans CJK SC", "Source Han Sans SC", "WenQuanYi Micro Hei", "Droid Sans Fallback" };

        public Task<byte[]> GenerateReportToPdf(ModObjectList ModObjects, string coverSvg, Action<string, int> Progress)
        {
            return Task.Run(async () =>
            {
                var Instance = ServiceSingleton.Instances.WorkingInstance;

                Progress?.Invoke("Preparing data", 5);

                // Gather dynamic strings exactly like the original
                string cpu = await ServiceSingleton.Globals.GetCPUInfo();
                string gpu = string.Join(Environment.NewLine, ServiceSingleton.Globals.GetVideoAdapters().ToArray());
                string ram = await ServiceSingleton.Globals.GetRamCount();
                var system = GetSystemConfiguration();

                int addedMods = ModObjects.AddedModsCount;
                int removedMods = ModObjects.RemovedModsCount;
                int versionMismatch = ModObjects.VersionMismatchCount;
                int disabledMods = ModObjects.DisabledModsCount;

                Progress?.Invoke("Composing PDF", 15);

                var doc = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // NOTE: If PageSizes is not available in your version, see note in section (3) below.
                        page.Size(PageSizes.A4);
                        page.Margin(0);
                        page.DefaultTextStyle(x => x.FontFamily(ReportFonts).FontColor(Colors.White));
                        page.PageColor(BgHex);

                        page.Content().PaddingHorizontal(36).PaddingVertical(28).Column(col =>
                        {
                            // Cover image, drawn as vector paths so it stays sharp at any zoom
                            if (!string.IsNullOrWhiteSpace(coverSvg))
                            {
                                col.Item().AlignCenter().Height(250).Width(450).Svg(SvgImage.FromText(coverSvg)).FitWidth();
                                col.Item().PaddingTop(8);
                            }

                            col.Item().PaddingTop(4);

                            AddRow(
                                col,
                                "Report Date : ",
                                string.Format(
                                    "{0} at {1}",
                                    DateTime.Now.ToString("MMMM dd, yyyy", new CultureInfo("en-US")),
                                    DateTime.Now.ToShortTimeString()
                                )
                            );

                            col.Item().PaddingTop(8);

                            // Sections: use small helpers for consistency
                            Progress?.Invoke("Hardware", 25);
                            AddHeader(col, "HARDWARE CONFIGURATION");
                            AddRow(col, "CPU : ", cpu);
                            AddRow(col, "GPU : ", gpu);
                            AddRow(col, "RAM : ", ram != "RAM count not found" ? $"{ram} GB" : ram);

                            Progress?.Invoke("System", 30);
                            col.Item().PaddingTop(12);
                            AddHeader(col, "SYSTEM CONFIGURATION");
                            foreach (var (label, value) in system)
                                AddRow(col, label, value);

                            Progress?.Invoke("Instance configuration", 35);
                            col.Item().PaddingTop(12);
                            AddHeader(col, "INSTANCE CONFIGURATION");
                            AddRow(col, "Name : ", $"{Instance.Name}, selected profile : {(ModObjects.Profile ?? "Default")}");
                            AddRow(col, "Version : ", Instance.Version);
                            AddRow(col, "Variant : ", Instance.SettingsManager.Variant);
                            AddRow(col, "Tag : ", Instance.Tag == string.Empty ? "-" : Instance.Tag);
                            AddRow(col, "Install directory : ", Instance.SettingsManager.InstallDir);
                            AddRow(col, "Archive directory : ", Instance.SettingsManager.ArchiveDir);
                            AddRow(col, "Enable archiving : ", Instance.SettingsManager.EnableArchiving ? "Yes" : "No");
                            AddRow(col, "Resolution : ", $"{Instance.SettingsManager.Width}x{Instance.SettingsManager.Height}");
                            AddRow(col, "Downscaling : ", Instance.SettingsManager.DownScaling == "TRUE" ? "Yes" : "No");
                            AddRow(col, "Resolution : ", Instance.SettingsManager.DownScaling == "TRUE" ? Instance.SettingsManager.DownScaledResolution : "-");

                            AddRow(col, "Ini level : ", Instance.SettingsManager.IniSettings switch
                            {
                                "0" => "Low",
                                "1" => "Medium",
                                "2" => "High",
                                _ => "Unknown"
                            });

                            AddRow(col, "CDN : ", Instance.SettingsManager.CDN);
                            AddRow(col, "Language : ", Instance.SettingsManager.LgName);

                            Progress?.Invoke("Options", 65);
                            col.Item().PageBreak();

                            foreach (var Category in Instance.OptionsManager.GetOptionCategories())
                            {
                                col.Item().PaddingTop(12);
                                AddHeader(col, string.Format("{0} Options", Category));

                                foreach (var Option in Instance.OptionsManager.GetOptionsByCategory(Category))
                                    AddRow(col, string.Format("{0} : ", Option.FriendlyName), Option.FormattedValue);
                            }

                            Progress?.Invoke("Status", 75);
                            col.Item().PaddingTop(12);
                            AddHeader(col, "INSTANCE STATUS");

                            if (addedMods > 0 || removedMods > 0 || disabledMods > 0)
                            {
                                AddRow(col, "List status : ", "List has been modified");
                            }
                            else if (ServiceSingleton.Packages.ModOrganizer2.GetProfiles().Count > 1)
                            {
                                AddRow(col, "List status : ", $"List might be modified ({ServiceSingleton.Packages.ModOrganizer2.GetProfiles().Count} profiles found)");
                                AddRow(col, "New profile(s) added : ", string.Join(",", ServiceSingleton.Packages.ModOrganizer2.GetProfiles().ToArray()));
                            }
                            else
                            {
                                AddRow(col, "List status : ", "Vanilla Nolvus");
                            }

                            AddRow(col, "Added mods : ", addedMods.ToString());
                            AddRow(col, "Removed mods : ", removedMods.ToString());
                            AddRow(col, "Version changed : ", versionMismatch.ToString());
                            AddRow(col, "Disabled mods : ", disabledMods.ToString());

                            // Lists (mod names), paginated automatically by QuestPDF
                            if (addedMods > 0)
                            {
                                col.Item().PageBreak();
                                AddHeader(col, "ADDED MODS");
                                foreach (var mod in ModObjects.AddedMods)
                                    AddTextOnly(col, $"{mod.Name} [{(mod.Selected ? "v" : "x")}]");
                            }

                            if (removedMods > 0)
                            {
                                col.Item().PageBreak();
                                AddHeader(col, "REMOVED MODS");
                                foreach (var mod in ModObjects.RemovedMods)
                                    AddTextOnly(col, mod.Name);
                            }

                            if (versionMismatch > 0)
                            {
                                col.Item().PageBreak();
                                AddHeader(col, "VERSION MISMATCH");
                                foreach (var mod in ModObjects.VersionMismatchMods)
                                    AddTextOnly(col, $"{mod.Name} , {mod.StatusText}");
                            }

                            if (disabledMods > 0)
                            {
                                col.Item().PageBreak();
                                AddHeader(col, "DISABLED MODS");
                                foreach (var mod in ModObjects.DisabledMods)
                                    AddTextOnly(col, mod.Name);
                            }
                        });
                    });
                });

                Progress?.Invoke("Rendering PDF", 95);

                // Render to memory
                using var ms = new MemoryStream();
                doc.GeneratePdf(ms);
                Progress?.Invoke("Done", 100);
                return ms.ToArray();
            });
        }

        // ---------- Helpers ----------

        private static void AddHeader(ColumnDescriptor col, string text)
        {
            col.Item().PaddingVertical(6).Element(container =>
            {
                container
                    .Background(OrangeHex)
                    .Border(1).BorderColor(Colors.White)
                    .PaddingVertical(16).PaddingHorizontal(12)
                    .AlignCenter()
                    .Text(text)
                    .FontSize(20).Bold().FontColor(Colors.White);
            });
        }

        private static void AddRow(ColumnDescriptor col, string label, string value)
        {
            col.Item().Text(txt =>
            {
                txt.Span(label).FontSize(10).FontColor(Colors.White);
                txt.Span(value ?? string.Empty).FontSize(10).FontColor(OrangeHex);
            });
        }

        private static void AddTextOnly(ColumnDescriptor col, string value)
        {
            col.Item().Text(value ?? string.Empty).FontSize(10).FontColor(Colors.White);
        }

        // ---------- System configuration ----------

        private sealed record MountEntry(string Device, string MountPoint, string FileSystem);

        private static List<(string Label, string Value)> GetSystemConfiguration()
        {
            var rows = new List<(string Label, string Value)>
            {
                ("Distribution : ", GetDistribution()),
                ("Kernel : ", GetKernel()),
                ("Desktop : ", GetDesktop()),
                ("Graphics driver : ", GetGraphicsDriver()),
                ("Swap : ", GetSwap()),
                ("Dashboard version : ", ServiceSingleton.Dashboard.Version)
            };

            rows.AddRange(GetDisks());

            return rows;
        }

        private static string GetDistribution()
        {
            foreach (var file in new[] { "/etc/os-release", "/usr/lib/os-release" })
            {
                try
                {
                    if (!File.Exists(file))
                        continue;

                    string? prettyName = null, name = null, version = null;

                    foreach (var line in File.ReadLines(file))
                    {
                        var idx = line.IndexOf('=');
                        if (idx <= 0)
                            continue;

                        var value = line[(idx + 1)..].Trim().Trim('"', '\'');

                        switch (line[..idx].Trim())
                        {
                            case "PRETTY_NAME": prettyName = value; break;
                            case "NAME": name = value; break;
                            case "VERSION": version = value; break;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(prettyName))
                        return prettyName;

                    if (!string.IsNullOrWhiteSpace(name))
                        return $"{name} {version}".Trim();
                }
                catch { }
            }

            return "Unknown";
        }

        private static string GetKernel()
        {
            try
            {
                return File.ReadAllText("/proc/sys/kernel/osrelease").Trim();
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetDesktop()
        {
            var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
            var session = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");

            if (string.IsNullOrWhiteSpace(session))
            {
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
                    session = "wayland";
                else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")))
                    session = "x11";
            }

            session = session?.ToLowerInvariant() switch
            {
                "wayland" => "Wayland",
                "x11" => "X11",
                _ => session
            };

            if (string.IsNullOrWhiteSpace(desktop))
                return string.IsNullOrWhiteSpace(session) ? "Unknown" : session;

            return string.IsNullOrWhiteSpace(session) ? desktop : $"{desktop} ({session})";
        }

        private static string GetGraphicsDriver()
        {
            try
            {
                var psi = new ProcessStartInfo("glxinfo", "-B")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using var proc = Process.Start(psi);
                var output = proc?.StandardOutput.ReadToEnd() ?? string.Empty;
                proc?.WaitForExit(2000);

                // OpenGL version string: 4.6 (Compatibility Profile) Mesa 25.2.4-arch1.1
                // OpenGL version string: 4.6.0 NVIDIA 580.95.05
                var match = Regex.Match(output, @"OpenGL version string:.*?\b(Mesa|NVIDIA)\s+(\S+)");
                if (match.Success)
                    return $"{match.Groups[1].Value} {match.Groups[2].Value}";
            }
            catch { }

            // glxinfo is not installed everywhere, but the NVIDIA kernel module reports its own version
            try
            {
                if (File.Exists("/sys/module/nvidia/version"))
                    return "NVIDIA " + File.ReadAllText("/sys/module/nvidia/version").Trim();
            }
            catch { }

            return "Unknown";
        }

        private static string GetSwap()
        {
            try
            {
                foreach (var line in File.ReadLines("/proc/meminfo"))
                {
                    if (line.StartsWith("SwapTotal"))
                    {
                        var kb = long.Parse(line.Split(':')[1].Replace("kB", "").Trim());
                        return kb == 0 ? "None" : FormatSize(kb * 1024);
                    }
                }
            }
            catch { }

            return "Unknown";
        }

        // One row per disk partition. A partition mounted more than once (btrfs subvolumes, bind mounts)
        // is listed at its shortest mount point; boot partitions and loop/zram devices are left out.
        private static List<(string Label, string Value)> GetDisks()
        {
            return GetMounts()
                .Where(m => m.Device.StartsWith("/dev/") && !m.Device.StartsWith("/dev/loop") && !m.Device.StartsWith("/dev/zram"))
                .Where(m => m.MountPoint != "/boot" && !m.MountPoint.StartsWith("/boot/") && m.MountPoint != "/efi")
                .GroupBy(m => m.Device)
                .Select(g => g.OrderBy(m => m.MountPoint.Length).First())
                .OrderBy(m => m.MountPoint, StringComparer.Ordinal)
                .Select(m => ("Disk : ", DescribeMount(m)))
                .ToList();
        }

        private static List<MountEntry> GetMounts()
        {
            var mounts = new List<MountEntry>();

            try
            {
                foreach (var line in File.ReadLines("/proc/self/mounts"))
                {
                    var parts = line.Split(' ');
                    if (parts.Length < 3)
                        continue;

                    mounts.Add(new MountEntry(UnescapeMountField(parts[0]), UnescapeMountField(parts[1]), parts[2]));
                }
            }
            catch { }

            return mounts;
        }

        // /proc/self/mounts writes spaces, tabs and backslashes in paths as octal escapes (\040 etc.)
        private static string UnescapeMountField(string value)
        {
            return Regex.Replace(value, @"\\([0-7]{3})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 8)).ToString());
        }

        private static string DescribeMount(MountEntry mount)
        {
            var space = "free space unknown";

            try
            {
                space = $"{FormatSize(new DriveInfo(mount.MountPoint).AvailableFreeSpace)} free";
            }
            catch { }

            return $"{mount.MountPoint} - {space} ({mount.FileSystem})";
        }

        private static string FormatSize(long bytes)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.#} GB", bytes / 1024.0 / 1024.0 / 1024.0);
        }
    }
}
