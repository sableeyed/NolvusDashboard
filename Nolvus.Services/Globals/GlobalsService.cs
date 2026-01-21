using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using System.Text.RegularExpressions;

namespace Nolvus.Services.Globals
{
    public class GlobalsService : IGlobalsService
    {
        public const string NolvusSection = "Nolvus";
        public const string UserName = "UserName";
        public const string Password = "Password";
        public const string Version = "Version";
        public const string NolvusApi = "https://www.Nolvus.net/rest/";
        public const string NexusSection = "Nexus";
        public const string ApiKey = "ApiKey";
        public const string UserAgent = "UserAgent";
        public const string MegaSection = "Mega";
        public const string MegaUserName = "Email";
        public const string MegaPswd = "Password";
        public const string MegaAnonymous = "AnonymousConnection";

        public string ApiVersion => ServiceSingleton.Settings.GetIniValue(NolvusSection, Version);
        public string ApiUrl => NolvusApi;
        public string NolvusUserName => ServiceSingleton.Settings.GetIniValue(NolvusSection, UserName);
        public string NolvusPassword => ServiceSingleton.Settings.GetIniValue(NolvusSection, Password);

        public bool MegaAnonymousConnection
        {
            get
            {
                var val = ServiceSingleton.Settings.GetIniValue(MegaSection, MegaAnonymous);
                return val == null ? true : Convert.ToBoolean(val);
            }
            set => ServiceSingleton.Settings.StoreIniValue(MegaSection, MegaAnonymous, value.ToString().Trim());
        }

        public string MegaEmail
        {
            get => ServiceSingleton.Settings.GetIniValue(MegaSection, MegaUserName) ?? string.Empty;
            set => ServiceSingleton.Settings.StoreIniValue(MegaSection, MegaUserName, value);
        }

        public string MegaPassword
        {
            get
            {
                var pass = ServiceSingleton.Settings.GetIniValue(MegaSection, MegaPswd);
                return pass == null ? string.Empty : ServiceSingleton.Lib.DecryptString(pass);
            }
            set => ServiceSingleton.Settings.StoreIniValue(MegaSection, MegaPswd, ServiceSingleton.Lib.EncryptString(value));
        }

        public string NexusApiKey => ServiceSingleton.Settings.GetIniValue(NexusSection, ApiKey);
        public string NexusUserAgent => ServiceSingleton.Settings.GetIniValue(NexusSection, UserAgent);

        public string NolvusUserAgent
        {
            get
            {
                var val = ServiceSingleton.Settings.GetIniValue(NolvusSection, UserAgent);
                if (string.IsNullOrWhiteSpace(val))
                {
                    const string DefaultUA = "Mozilla/5.0 (X11; Linux x86_64) NolvusleWebKit/537.36 Chrome/115 Safari/537.36";
                    ServiceSingleton.Settings.StoreIniValue(NolvusSection, UserAgent, DefaultUA);
                    return DefaultUA;
                }
                return val;
            }
        }

        public List<string> WindowsResolutions
        {
            get
            {
                var results = new HashSet<string>(StringComparer.Ordinal);

                try
                {
                    var psi = new ProcessStartInfo("xrandr", "--current")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };

                    using var proc = Process.Start(psi);
                    string output = proc?.StandardOutput.ReadToEnd() ?? "";
                    proc?.WaitForExit(2000);

                    var headerRegex = new Regex(@"^(?<name>\S+)\s+connected\b", RegexOptions.Compiled);

                    string? currentOutput = null;

                    foreach (var raw in output.Split('\n'))
                    {
                        var line = raw.TrimEnd();

                        var hm = headerRegex.Match(line);
                        if (hm.Success)
                        {
                            currentOutput = hm.Groups["name"].Value;
                            continue;
                        }

                        if (currentOutput == null)
                            continue;

                        if (line.Contains("*"))
                        {
                            var res = line.Trim().Split(' ')[0];
                            results.Add(res);
                            currentOutput = null;
                        }
                    }
                }
                catch
                {
                    results.Clear();
                }

                results.Add("1920x1080");

                if (results.Count == 0)
                    results.Add("Resolution info not available");

                return results
                    .Select(r =>
                    {
                        var p = r.Split('x');
                        return new
                        {
                            Text = r,
                            W = int.TryParse(p[0], out var w) ? w : 0,
                            H = int.TryParse(p[1], out var h) ? h : 0
                        };
                    })
                    .OrderByDescending(x => x.W * x.H)
                    .ThenByDescending(x => x.W)
                    .Select(x => x.Text)
                    .ToList();
            }
        }

        // UPDATED GPU DETECTION LOGIC
        public List<string> GetVideoAdapters()
        {
            var results = new List<string>();

            try
            {
                // ---- First Attempt: glxinfo (Device) ----
                var psi = new ProcessStartInfo("bash", "-c \"glxinfo | grep -i 'Device'\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using var proc = Process.Start(psi);
                var output = proc.StandardOutput.ReadToEnd().Trim();

                string model = ExtractModelFromGlxinfo(output);

                // If still not found, try OpenGL Renderer String:
                if (string.IsNullOrWhiteSpace(model) || model == "Unknown GPU")
                {
                    var psi2 = new ProcessStartInfo("bash", "-c \"glxinfo | grep -i 'OpenGL renderer string'\"")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };

                    using var proc2 = Process.Start(psi2);
                    var fallbackOut = proc2.StandardOutput.ReadToEnd().Trim();

                    // Example:
                    // OpenGL renderer string: AMD Radeon RX 9070 XT / radeonsi
                    var idx = fallbackOut.IndexOf(':');
                    if (idx != -1)
                    {
                        var tmp = fallbackOut.Substring(idx + 1).Trim();
                        var slash = tmp.IndexOf('/');
                        if (slash != -1)
                            tmp = tmp.Substring(0, slash).Trim();

                        model = tmp;
                    }
                }

                // If still unknown, NVIDIA fallback only (as requested)
                if (string.IsNullOrWhiteSpace(model) || model == "Unknown GPU")
                {
                    var psi3 = new ProcessStartInfo("bash", "-c \"lspci | grep -i nvidia\"")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };

                    using var proc3 = Process.Start(psi3);
                    var out3 = proc3.StandardOutput.ReadToEnd().Trim();

                    if (!string.IsNullOrWhiteSpace(out3))
                    {
                        var m = ExtractModelFromGlxinfo(out3);
                        if (!string.IsNullOrWhiteSpace(m))
                            model = m;
                    }
                }

                if (!string.IsNullOrWhiteSpace(model))
                    results.Add(model);
                else
                    results.Add("Unknown GPU");
            }
            catch
            {
                results.Add("Unknown GPU");
            }

            return results;
        }

        private string ExtractModelFromGlxinfo(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return "Unknown GPU";

            var idx = line.IndexOf("Device:");
            if (idx != -1)
                line = line.Substring(idx + "Device:".Length).Trim();

            var p = line.IndexOf("(");
            if (p != -1)
                line = line.Substring(0, p).Trim();

            line = line.Replace("  ", " ").Trim();

            return line;
        }

        public async Task<string> GetCPUInfo()
        {
            return await Task.Run(() =>
            {
                try
                {
                    foreach (var line in File.ReadLines("/proc/cpuinfo"))
                    {
                        if (line.StartsWith("model name"))
                            return line.Split(':')[1].Trim();
                    }
                    return "CPU info not found";
                }
                catch
                {
                    return "CPU info not found";
                }
            });
        }

        public async Task<string> GetRamCount()
        {
            return await Task.Run(() =>
            {
                try
                {
                    foreach (var line in File.ReadLines("/proc/meminfo"))
                    {
                        if (line.StartsWith("MemTotal"))
                        {
                            var kb = long.Parse(line.Split(':')[1].Replace("kB", "").Trim());
                            var gb = kb / 1024.0 / 1024.0;
                            return ((int)Math.Floor(gb)).ToString();
                        }
                    }
                    return "RAM count not found";
                }
                catch
                {
                    return "RAM count not found";
                }
            });
        }

        public string GetVersion(string filePath)
        {
            var asm = Assembly.GetEntryAssembly();
            if (asm == null)
                return "0.0.0";

            var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? asm.GetName().Version?.ToString()
                    ?? "0.0.0";

            var plusIndex = version.IndexOf('+');
            if (plusIndex > 0)
                version = version[..plusIndex];

            return version;
        }

        public List<string> GetDownscaleResolutions()
        {
            var inst = ServiceSingleton.Instances.WorkingInstance;

            if (!int.TryParse(inst.Settings.Width, out var w) || !int.TryParse(inst.Settings.Height, out var h) || w <= 0 || h <= 0)
                return new List<string> { "1920x1080" };

            const int floorW = 1920;
            const int floorH = 1080;

            var results = new HashSet<string>(StringComparer.Ordinal);

            try
            {
                var psi = new ProcessStartInfo("xrandr", "--current")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using var proc = Process.Start(psi);
                string output = proc?.StandardOutput.ReadToEnd() ?? "";
                proc?.WaitForExit(2000);

                var headerRegex = new Regex(@"^(?<name>\S+)\s+connected\b", RegexOptions.Compiled);
                var modeRegex = new Regex(@"^\s*(\d{3,5}x\d{3,5})\s", RegexOptions.Compiled);

                bool inConnectedBlock = false;

                foreach (var raw in output.Split('\n'))
                {
                    var line = raw.TrimEnd();

                    if (headerRegex.IsMatch(line))
                    {
                        inConnectedBlock = true;
                        continue;
                    }

                    if (line.Contains(" disconnected"))
                    {
                        inConnectedBlock = false;
                        continue;
                    }

                    if (!inConnectedBlock)
                        continue;

                    var mm = modeRegex.Match(line);
                    if (!mm.Success)
                        continue;

                    var res = mm.Groups[1].Value;

                    // Exclude the primary/current selected monitor resolution (no-op downscale)
                    if (res == $"{w}x{h}")
                        continue;

                    var parts = res.Split('x');
                    if (parts.Length != 2)
                        continue;

                    if (!int.TryParse(parts[0], out var rw) || !int.TryParse(parts[1], out var rh))
                        continue;

                    // Enforce floor
                    if (rw < floorW || rh < floorH)
                        continue;

                    results.Add(res);
                }
            }
            catch {}

            // Always include the floor option
            results.Add("1920x1080");

            return results
                .Select(r =>
                {
                    var p = r.Split('x');
                    int rw = int.Parse(p[0]);
                    int rh = int.Parse(p[1]);
                    return new { Text = r, W = rw, H = rh, Pixels = rw * rh };
                })
                .OrderByDescending(x => x.Pixels)
                .ThenByDescending(x => x.W)
                .Select(x => x.Text)
                .ToList();
        }
    }
}
