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

        public Task<string> GenerateReportToClipBoard(ModObjectList ModObjects, Action<string, int> Progress)
        {
            return Task.Run(async () =>
            {
                var Instance = ServiceSingleton.Instances.WorkingInstance;

                Progress?.Invoke("Generating report", 10);

                var sb = new StringBuilder();

                // HARDWARE
                sb.AppendLine("HARDWARE");
                sb.AppendLine();
                sb.AppendLine($"CPU : {await ServiceSingleton.Globals.GetCPUInfo()}");
                sb.AppendLine("GPU : " + string.Join(Environment.NewLine, ServiceSingleton.Globals.GetVideoAdapters().ToArray()));

                var ram = await ServiceSingleton.Globals.GetRamCount();
                sb.AppendLine(ram != "RAM count not found" ? $"RAM : {ram} GB" : ram);
                sb.AppendLine().AppendLine();

                // SYSTEM
                sb.AppendLine("SYSTEM");
                sb.AppendLine();
                foreach (var (label, value) in GetSystemConfiguration())
                    sb.AppendLine(label + value);
                sb.AppendLine().AppendLine();

                Progress?.Invoke("Generating report", 20);

                // INSTANCE CONFIGURATION
                sb.AppendLine("INSTANCE CONFIGURATION");
                sb.AppendLine();
                sb.AppendLine($"Name : {Instance.Name}, selected profile : {ModObjects.Profile}");
                sb.AppendLine($"Version : {Instance.Version}");
                sb.AppendLine($"Install directory : {Instance.InstallDir}");
                sb.AppendLine($"Archive directory : {Instance.ArchiveDir}");
                sb.AppendLine($"Enable archiving : {(Instance.Settings.EnableArchiving ? "Yes" : "No")}");
                sb.AppendLine().AppendLine();

                Progress?.Invoke("Generating report", 30);

                // INSTANCE SETTINGS
                sb.AppendLine("INSTANCE SETTINGS");
                sb.AppendLine();
                sb.AppendLine($"Resolution : {Instance.Settings.Width}x{Instance.Settings.Height}");
                sb.AppendLine($"Screen ratio : {Instance.Settings.Ratio}");
                sb.AppendLine($"CDN : {Instance.Settings.CDN}");
                sb.AppendLine($"Language : {Instance.Settings.LgName}");
                sb.AppendLine().AppendLine();

                Progress?.Invoke("Generating report", 40);

                // PERFORMANCE (V5 vs V6 logic preserved)
                sb.AppendLine("PERFORMANCE SETTINGS");
                sb.AppendLine();

                if (Instance.Name == Strings.NolvusAscension)
                {
                    sb.AppendLine($"Variant : {Instance.Performance.Variant}");
                    sb.AppendLine($"Anti aliasing : {Instance.Performance.AntiAliasing}");
                    sb.AppendLine("Lods : " + (Instance.Performance.Variant == "Redux" ? "Redux" : Instance.Performance.LODs));

                    sb.AppendLine("Ini level : " + Instance.Performance.IniSettings switch
                    {
                        "0" => "Low",
                        "1" => "Medium",
                        "2" => "High",
                        _ => "Unknown"
                    });

                    sb.AppendLine($"Advanced Physics : {(Instance.Performance.AdvancedPhysics == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Ray tracing : {(Instance.Performance.RayTracing == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"FPS Stabilizer : {(Instance.Performance.FPSStabilizer == "TRUE" ? "Yes" : "No")}");
                    if (Instance.Performance.DownScaling == "TRUE")
                    {
                        sb.AppendLine("Downscaling : Yes");
                        sb.AppendLine($"Downscaling resolution : {Instance.Performance.DownWidth}x{Instance.Performance.DownHeight}");
                    }
                    else sb.AppendLine("Downscaling : No");
                }
                else if (Instance.Name == Strings.NolvusAwakening)
                {
                    sb.AppendLine($"Variant : {Instance.Performance.Variant}");
                    sb.AppendLine($"Anti aliasing : {Instance.Performance.AntiAliasing}");
                    if (Instance.Performance.Variant == "Redux")
                        sb.AppendLine("Lods : Redux");
                    else if (Instance.Performance.Variant == "Ultimate")
                        sb.AppendLine("Lods : Ultimate");
                    else
                        sb.AppendLine($"Lods : {Instance.Performance.LODs}");

                    sb.AppendLine("Ini level : " + Instance.Performance.IniSettings switch
                    {
                        "0" => "Low",
                        "1" => "Medium",
                        "2" => "High",
                        _ => "Unknown"
                    });

                    sb.AppendLine($"SR Exterior Cities : {(Instance.Performance.SREX == "TRUE" ? "Yes" : "No")}");

                    sb.AppendLine($"Frame Generation : {(Instance.Performance.FrameGeneration == "TRUE" ? "Yes" : "No")}");

                    if (Instance.Performance.DownScaling == "TRUE")
                    {
                        sb.AppendLine("Downscaling : Yes");
                        sb.AppendLine($"Downscaling resolution : {Instance.Performance.DownWidth}x{Instance.Performance.DownHeight}");
                    }
                    else sb.AppendLine("Downscaling : No");
                }

                sb.AppendLine().AppendLine();
                Progress?.Invoke("Generating report", 50);

                // ENB
                sb.AppendLine("ENB SETTINGS");
                sb.AppendLine();
                sb.AppendLine("ENB Preset : " + ENBs.GetENBByCode(Instance.Options.AlternateENB));
                sb.AppendLine().AppendLine();

                Progress?.Invoke("Generating report", 60);

                // OPTIONS
                sb.AppendLine("INSTANCE OPTIONS");
                sb.AppendLine();

                if (Instance.Name == Strings.NolvusAscension)
                {
                    sb.AppendLine($"Hardcore mode : {(Instance.Options.HardcoreMode == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Alternate leveling : {(Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Alternate start : {(Instance.Options.AlternateStart == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Fantasy mode : {(Instance.Options.FantasyMode == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Nudity : {(Instance.Options.Nudity == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Skin type : {Instance.Options.SkinType}");
                }
                else if (Instance.Name == Strings.NolvusAwakening)
                {
                    sb.AppendLine($"Combat Animations : {Instance.Options.CombatAnimation}");
                    sb.AppendLine($"Alternate leveling : {(Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No")}");
                    //sb.AppendLine($"Alternate start : {(Instance.Options.AlternateStart == "TRUE" ? "Yes" : "No")}");
                    //sb.AppendLine($"Stances Perks Tree : {(Instance.Options.StancesPerksTree == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Nudity : {(Instance.Options.Nudity == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Gore : {(Instance.Options.Gore == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Combat Scaling : {Instance.Options.CombatScaling}");
                    sb.AppendLine($"Exhaustion : {(Instance.Options.Exhaustion == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Nerf Power Attacks : {Instance.Options.NerfPA}");
                    //sb.AppendLine($"Enemies Resistance : {(Instance.Options.EnemiesResistance == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Boss : {(Instance.Options.Boss == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Zone Level Indicator : {(Instance.Options.ZoneLevelIndicator == "TRUE" ? "Yes" : "No")}");
                    //sb.AppendLine($"Poise : {(Instance.Options.Poise == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"Controller : {(Instance.Options.Controller == "TRUE" ? "Yes" : "No")}");
                    sb.AppendLine($"User Interface : {Instance.Options.UI}");
                }

                sb.AppendLine().AppendLine();
                Progress?.Invoke("Generating report", 70);

                // STATUS
                sb.AppendLine("INSTANCE STATUS");
                sb.AppendLine();

                var addedMods = ModObjects.AddedModsCount;
                var removedMods = ModObjects.RemovedModsCount;
                var versionMismatch = ModObjects.VersionMismatchCount;

                if (addedMods > 0 || removedMods > 0)
                    sb.AppendLine("List has been modified");
                else
                    sb.AppendLine("Vanilla Nolvus");

                sb.AppendLine();
                sb.AppendLine($"Added mods : {addedMods}");
                sb.AppendLine($"Removed mods : {removedMods}");
                sb.AppendLine($"Version changed : {versionMismatch}");

                Progress?.Invoke("Generating report", 100);

                return sb.ToString();
            });
        }

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
                            AddRow(col, "Install directory : ", Instance.InstallDir);
                            AddRow(col, "Archive directory : ", Instance.ArchiveDir);
                            AddRow(col, "Enable archiving : ", Instance.Settings.EnableArchiving ? "Yes" : "No");

                            col.Item().PaddingTop(12);
                            AddHeader(col, "INSTANCE SETTINGS");
                            AddRow(col, "Resolution : ", $"{Instance.Settings.Width}x{Instance.Settings.Height}");
                            AddRow(col, "Screen ratio : ", Instance.Settings.Ratio);
                            AddRow(col, "CDN : ", Instance.Settings.CDN);
                            AddRow(col, "Language : ", Instance.Settings.LgName);

                            Progress?.Invoke("Performance", 45);
                            col.Item().PaddingTop(12);
                            AddHeader(col, "PERFORMANCE SETTINGS");

                            if (Instance.Name == Strings.NolvusAscension)
                            {
                                AddRow(col, "Variant : ", Instance.Performance.Variant);
                                AddRow(col, "Anti aliasing : ", Instance.Performance.AntiAliasing);
                                AddRow(col, "Lods : ", Instance.Performance.Variant == "Redux" ? "Redux" : Instance.Performance.LODs);

                                AddRow(col, "Ini level : ", Instance.Performance.IniSettings switch
                                {
                                    "0" => "Low",
                                    "1" => "Medium",
                                    "2" => "High",
                                    _ => "Unknown"
                                });

                                AddRow(col, "Advanced Physics : ", Instance.Performance.AdvancedPhysics == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Ray tracing : ", Instance.Performance.RayTracing == "TRUE" ? "Yes" : "No");
                                AddRow(col, "FPS Stabilizer : ", Instance.Performance.FPSStabilizer == "TRUE" ? "Yes" : "No");

                                if (Instance.Performance.DownScaling == "TRUE")
                                {
                                    AddRow(col, "Downscaling : ", "Yes");
                                    AddRow(col, "Downscaling resolution : ", $"{Instance.Performance.DownWidth}x{Instance.Performance.DownHeight}");
                                }
                                else
                                    AddRow(col, "Downscaling : ", "No");
                            }
                            else if (Instance.Name == Strings.NolvusAwakening)
                            {
                                AddRow(col, "Variant : ", Instance.Performance.Variant);
                                AddRow(col, "Anti aliasing : ", Instance.Performance.AntiAliasing);

                                if (Instance.Performance.Variant == "Redux")
                                    AddRow(col, "Lods : ", "Redux");
                                else if (Instance.Performance.Variant == "Ultimate")
                                    AddRow(col, "Lods : ", "Ultimate");
                                else
                                    AddRow(col, "Lods : ", Instance.Performance.LODs);

                                AddRow(col, "Ini level : ", Instance.Performance.IniSettings switch
                                {
                                    "0" => "Low",
                                    "1" => "Medium",
                                    "2" => "High",
                                    _ => "Unknown"
                                });

                                AddRow(col, "SR Exterior Cities : ", Instance.Performance.SREX == "TRUE" ? "Yes" : "No");
                                
                                AddRow(col, "Frame Generation : ", Instance.Performance.FrameGeneration == "TRUE" ? "Yes" : "No");

                                if (Instance.Performance.DownScaling == "TRUE")
                                {
                                    AddRow(col, "Downscaling : ", "Yes");
                                    AddRow(col, "Downscaling resolution : ", $"{Instance.Performance.DownWidth}x{Instance.Performance.DownHeight}");
                                }
                                else
                                    AddRow(col, "Downscaling : ", "No");
                            }

                            Progress?.Invoke("ENB", 55);
                            col.Item().PaddingTop(12);
                            AddHeader(col, "ENB SETTINGS");
                            AddRow(col, "ENB Preset : ", ENBs.GetENBByCode(Instance.Options.AlternateENB));

                            Progress?.Invoke("Options", 65);
                            col.Item().PaddingTop(12);
                            AddHeader(col, "INSTANCE OPTIONS");

                            if (Instance.Name == Strings.NolvusAscension)
                            {
                                AddRow(col, "Hardcore mode : ", Instance.Options.HardcoreMode == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Alternate leveling : ", Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Alternate start : ", Instance.Options.AlternateStart == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Fantasy mode : ", Instance.Options.FantasyMode == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Nudity : ", Instance.Options.Nudity == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Skin type : ", Instance.Options.SkinType);
                            }
                            else if (Instance.Name == Strings.NolvusAwakening)
                            {
                                AddRow(col, "Combat Animations : ", Instance.Options.CombatAnimation);
                                AddRow(col, "Alternate leveling : ", Instance.Options.AlternateLeveling == "TRUE" ? "Yes" : "No");
                                //AddRow(col, "Alternate start : ", Instance.Options.AlternateStart == "TRUE" ? "Yes" : "No");
                                //AddRow(col, "Stances Perks Tree : ", Instance.Options.StancesPerksTree == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Nudity : ", Instance.Options.Nudity == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Gore : ", Instance.Options.Gore == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Combat Scaling : ", Instance.Options.CombatScaling);
                                AddRow(col, "Exhaustion : ", Instance.Options.Exhaustion == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Nerf power attacks : ", Instance.Options.NerfPA);
                                //AddRow(col, "Enemies resistance : ", Instance.Options.EnemiesResistance == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Boss : ", Instance.Options.Boss == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Zone Level Indicator : ", Instance.Options.ZoneLevelIndicator == "TRUE" ? "Yes" : "No");
                                //AddRow(col, "Poise system : ", Instance.Options.Poise == "TRUE" ? "Yes" : "No");
                                AddRow(col, "Controller : ", Instance.Options.Controller == "TRUE" ? "Yes" : "No");
                                AddRow(col, "User Interface : ", Instance.Options.UI);
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
