using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

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
                var resolutions = new HashSet<string>();

                try
                {
                    var psi = new ProcessStartInfo("xrandr", "")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };

                    using var proc = Process.Start(psi);
                    string output = proc?.StandardOutput.ReadToEnd() ?? "";

                    foreach (var line in output.Split('\n'))
                    {
                        if (line.Contains(" connected"))
                        {
                        }
                        else if (line.Contains("*"))
                        {
                            var parts = line.Trim().Split(' ')[0];
                            resolutions.Add(parts);
                        }
                    }
                }
                catch
                {
                    resolutions.Add("Resolution info not available");
                }

                return new List<string>(resolutions);
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
    }
}
