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
            set => ServiceSingleton.Settings.StoreIniValue(MegaSection, MegaAnonymous, value.ToString());
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

        // Linux replacement for Windows display mode enumeration
        public List<string> WindowsResolutions
        {
            get
            {
                var resolutions = new HashSet<string>();

                try
                {
                    // Parse xrandr output if available
                    var psi = new ProcessStartInfo("xrandr", "")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                        // no window spawn, works headless
                    };

                    using var proc = Process.Start(psi);
                    string output = proc?.StandardOutput.ReadToEnd() ?? "";

                    foreach (var line in output.Split('\n'))
                    {
                        if (line.Contains(" connected"))
                        {
                            // Skip
                        }
                        else if (line.Contains("*")) // current mode marked with *
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

        public List<string> GetVideoAdapters()
        {
            var results = new List<string>();

            try
            {
                if (File.Exists("/proc/driver/nvidia/version"))
                    results.Add("NVIDIA GPU");

                // Read from PCI list
                var psi = new ProcessStartInfo("lspci", "")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using var proc = Process.Start(psi);
                var output = proc.StandardOutput.ReadToEnd();

                foreach (var line in output.Split('\n'))
                {
                    if (line.ToLower().Contains("vga"))
                        results.Add(line.Trim());
                }
            }
            catch
            {
                results.Add("GPU info not found");
            }

            return results;
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
                            var gb = kb / 1024 / 1024;
                            return gb.ToString();
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

            // Strip trailing build metadata like +commitHash if present
            var plusIndex = version.IndexOf('+');
            if (plusIndex > 0)
                version = version[..plusIndex];

            return version;
        }
    }
}
