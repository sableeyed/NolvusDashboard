using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Frames;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Vcc.Nolvus.Api.Installer.Library;
using Vcc.Nolvus.Api.Installer.Services;
using Nolvus.Package.Conditions;
using Nolvus.Dashboard.Controls;
using Nolvus.Dashboard.Frames.Installer;
using Nolvus.Dashboard.Frames.Instance;
using Nolvus.Dashboard.Frames.Settings;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nolvus.Dashboard.Frames
{
    public partial class StartFrame : DashboardFrame
    {
        public StartFrame(IDashboard dashboard, FrameParameters parameters)
            : base(dashboard, parameters)
        {
            InitializeComponent();
        }

        public StartFrame() : this(ServiceSingleton.Dashboard, new FrameParameters()) { }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                //Debugging
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var args = desktop.Args ?? Array.Empty<string>();

                    var index = Array.FindIndex(args, arg => string.Equals(arg, "--debugging", StringComparison.OrdinalIgnoreCase));

                    if (index >= 0 && args.Length > index + 1)
                    {
                        var frame = args[index + 1];
                        ServiceSingleton.Logger.Log($"[DEBUG] skipping normal startup -> {frame}");

                        var frameType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(arg =>
                            {
                                try 
                                {
                                    return arg.GetTypes();
                                }
                                catch 
                                {
                                    return Array.Empty<Type>();
                                }
                            }).FirstOrDefault(type =>
                                string.Equals(type.Name, frame, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(type.FullName, frame, StringComparison.OrdinalIgnoreCase));

                        if (frameType == null)
                        {
                            await ServiceSingleton.Dashboard.Error("Error", $"[DEBUG] invalid frame specified: {frame}");
                            return;
                        }

                        var dash = ServiceSingleton.Dashboard;
                        var dashType = dash.GetType();

                        MethodInfo? methodDef =
                            dashType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                .Where(m => m.Name == "LoadFrameAsync" && m.IsGenericMethodDefinition)
                                .OrderBy(m => m.GetParameters().Length)
                                .FirstOrDefault();

                        object?[]? invokeArgs = null;

                        if (methodDef == null)
                        {
                            methodDef = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(arg =>
                                {
                                    try { return arg.GetTypes(); }
                                    catch { return Array.Empty<Type>(); }
                                })
                                .Where(type => type.IsSealed && type.IsAbstract)
                                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                                .Where(method =>
                                    method.Name == "LoadFrameAsync" &&
                                    method.IsGenericMethodDefinition &&
                                    method.IsDefined(typeof(ExtensionAttribute), inherit: false)).Where(method =>
                                {
                                    var param = method.GetParameters();
                                    return param.Length >= 1 && param[0].ParameterType.IsAssignableFrom(dashType);
                                }).OrderBy(method => method.GetParameters().Length).FirstOrDefault();

                            if (methodDef == null)
                            {
                                await ServiceSingleton.Dashboard.Error("Error", "[DEBUG] Could not find LoadFrameAsync<T> method (instance or extension).");
                                return;
                            }
                        }

                        var parameters = methodDef.GetParameters();
                        if (methodDef.IsDefined(typeof(ExtensionAttribute), false))
                        {
                            invokeArgs = new object?[parameters.Length];
                            invokeArgs[0] = dash;

                            for (int i = 1; i < parameters.Length; i++)
                                invokeArgs[i] = parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null;
                        }
                        else
                        {
                            invokeArgs = new object?[parameters.Length];
                            for (int i = 0; i < parameters.Length; i++)
                                invokeArgs[i] = parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null;
                        }

                        var constructed = methodDef.MakeGenericMethod(frameType);

                        var result = constructed.Invoke(
                            methodDef.IsDefined(typeof(ExtensionAttribute), false) ? null : dash,
                            invokeArgs);

                        await (Task)result!;

                        ServiceSingleton.Dashboard.NoStatus();
                        ServiceSingleton.Dashboard.ProgressCompleted();
                        return;
                    }
                }

                if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NolvusDashboard.ini")))
                {
                    ServiceSingleton.Dashboard.LoadFrame<GameFrame>();
                }
                else
                {   
                    var window = TopLevel.GetTopLevel(this) as DashboardWindow;
                    window?.SetAccountImageVisible(true);
                    await CheckNolvus();
                    await CheckForUpdates();
                    await CheckNexus();

                    var InstancesCheck = await CheckInstances();
                    ServiceSingleton.Dashboard.EnableSettings();
                    switch (InstancesCheck)
                    {
                        case InstanceCheck.NoInstance:
                            ServiceSingleton.Logger.Log("Dashboard is ready to install");
                            await ServiceSingleton.Dashboard.LoadFrameAsync<SelectInstanceFrame>();
                            break;
                        case InstanceCheck.InstancesToResume:
                            ServiceSingleton.Logger.Log("Dashboard is ready to resume");
                            ServiceSingleton.Dashboard.LoadFrame<ResumeFrame>();
                            break;
                        case InstanceCheck.InstalledInstances:
                            ServiceSingleton.Logger.Log("Dashboard ready to play");
                            ServiceSingleton.Dashboard.LoadFrame<InstancesFrame>();
                            break;
                    }
                }
            } catch (Exception ex)
            {
                ServiceSingleton.Logger.Log(ex.StackTrace);
            }

            ServiceSingleton.Dashboard.NoStatus();
            ServiceSingleton.Dashboard.ProgressCompleted();

        }

        private async Task CheckNolvus()
        {
            try 
            {
                ServiceSingleton.Dashboard.Progress(0);
                ServiceSingleton.Dashboard.Status("Connecting to Nolvus...");
                ServiceSingleton.Logger.Log("Connecting to Nolvus...");

                var ApiUrl = ServiceSingleton.Globals.ApiUrl;
                var ApiVersion = ServiceSingleton.Globals.ApiVersion;
                var UserName = ServiceSingleton.Globals.NolvusUserName;
                var Password = ServiceSingleton.Globals.NolvusPassword;

                if (ApiVersion == string.Empty || UserName == string.Empty || Password == string.Empty)
                {
                    throw new Exception("Nolvus settings missing, please check your Nolvus settings!");
                }
                else
                {
                    ApiManager.Init(ApiUrl, ApiVersion, UserName, ServiceSingleton.Lib.DecryptString(Password));

                    if (!await ApiManager.Service.Installer.Authenticate(UserName, ServiceSingleton.Lib.DecryptString(Password)))
                    {
                        throw new Exception("Invalid Nolvus user name / password or your account has not been activated!");
                    }

                    ServiceSingleton.Dashboard.Progress(25);

                    ServiceSingleton.Logger.Log("Connected to Nolvus");
                }
            }
            catch (Exception ex) 
            {
                Exception CaughtExeption = ex;

                if (ex.InnerException != null)
                {
                    CaughtExeption = ex.InnerException;
                }
                
                throw new Exception("Error during Nolvus connection. The Nolvus web site may have issues currently. Original message : " + CaughtExeption + ")");
            }
        }

        private async Task CheckForUpdates()
        {
            try
            {
                ServiceSingleton.Dashboard.Status("Checking for updates...");
                ServiceSingleton.Logger.Log("Checking for updates...");
                //var latest = await ApiManager.Service.Installer.GetLatestInstaller(); //ignore upstream release info so we can check against our own
                var latest = await GetLatestReleaseTag();
                ServiceSingleton.Dashboard.Progress(50);

                if (ServiceSingleton.Dashboard.IsOlder(latest))
                {
                    ServiceSingleton.Logger.Log(string.Format("New Dashboard version available : {0}", latest));
                    ServiceSingleton.Logger.Log("Launching updater...");

                    LaunchUpdater(
                        installDir: AppContext.BaseDirectory,
                        targetVersion: latest
                    );

                    ServiceSingleton.Logger.Log("Closing application...");
                    ServiceSingleton.Dashboard.ShutDown();
                }
                else
                {
                    ServiceSingleton.Logger.Log("Nolvus Dashboard installer is up to date");
                }
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log("Update check failed: " + ex);
            }
        }

        private async Task CheckNexus()
        {
            ServiceSingleton.Dashboard.Status("Connecting to Nexus...");

            ServiceSingleton.Files.RemoveDirectory(ServiceSingleton.Folders.NexusCacheDirectory, false);

            var key = ServiceSingleton.Globals.NexusApiKey;
            var agent = ServiceSingleton.Globals.NexusUserAgent;

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(agent))
                throw new Exception("Nexus API key or User Agent missing!");

            NexusApi.ApiManager.Init(key, agent, ServiceSingleton.Folders.NexusCacheDirectory);

            ServiceSingleton.Dashboard.TitleInfo(NexusApi.ApiManager.AccountInfo.Name);
            ServiceSingleton.Dashboard.NexusAccount(ApiManager.Service.Installer.LoggedUser + "@" + NexusApi.ApiManager.AccountInfo.Name);

            if (NexusApi.ApiManager.AccountInfo.IsPremium)
                ServiceSingleton.Dashboard.AccountType("(Premium)");
            else if (NexusApi.ApiManager.AccountInfo.IsSupporter)
                ServiceSingleton.Dashboard.AccountType("(Supporter)");
            else
                ServiceSingleton.Dashboard.AccountType("(Default)");

            ServiceSingleton.Dashboard.Progress(75);
        }

        private async Task<InstanceCheck> CheckInstances()
        {
            try 
            {
                ServiceSingleton.Dashboard.Status("Checking instances...");
                ServiceSingleton.Logger.Log("Checking instances...");

                ServiceSingleton.Instances.Load();

                if (!ServiceSingleton.Instances.Empty)
                {
                    var InstanceMessage = string.Empty;

                    if (ServiceSingleton.Instances.CheckInstances(out InstanceMessage))
                    {
                        if (ServiceSingleton.Instances.InstancesToResume.Count > 0)
                        {
                            return InstanceCheck.InstancesToResume;
                        }
                        else
                        {
                            return InstanceCheck.InstalledInstances;
                        }
                    }
                    else
                    {
                        throw new Exception(InstanceMessage + ".This can happen if you modified the file InstancesData.xml manually!");
                    }
                }
                return InstanceCheck.NoInstance;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception("Error during instance checking with error : " + ex.Message + ". Certainly due to a manual editing of the InstancesData.xml file!");
            }
        }

        private async Task<string> GetLatestReleaseTag()
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };

            using var client = new HttpClient(handler);
            using var response = await client.GetAsync("https://github.com/sableeyed/NolvusDashboard/releases/latest", HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();
            var uri = response.RequestMessage!.RequestUri!;
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var version = segments[^1];
            return version;
        }

        private static void LaunchUpdater(string installDir, string targetVersion)
        {
            installDir = Path.GetFullPath(installDir);

            var updaterExe = Path.Combine(installDir, "NolvusUpdater");

            var psi = new ProcessStartInfo
            {
                FileName = updaterExe,
                WorkingDirectory = installDir,
                UseShellExecute = false
            };

            psi.ArgumentList.Add("--install-dir");
            psi.ArgumentList.Add(installDir);

            psi.ArgumentList.Add("--version");
            psi.ArgumentList.Add(targetVersion);

            psi.ArgumentList.Add("--pid");
            psi.ArgumentList.Add(Environment.ProcessId.ToString());

            Process.Start(psi);
        }
    }
}
