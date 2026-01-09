using Avalonia.Interactivity;
using Avalonia.Threading;
using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Enums;
using Nolvus.Core.Events;
using Nolvus.Instance.Core;
using Nolvus.Dashboard.Core;
using Nolvus.Dashboard.Controls;
using Nolvus.Browser;
using Nolvus.Components.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nolvus.Dashboard.Frames.Installer
{
    public partial class InstallFrame : DashboardFrame
    {
        private CancellationTokenSource? _refreshCts;

        public InstallFrame(IDashboard Dashboard, FrameParameters Params) 
            : base(Dashboard, Params)
        {
            InitializeComponent();

            ModsBox.ScalingFactor = ServiceSingleton.Dashboard.ScalingFactor;
        }

        public List<IInstallableElement> ModsToInstall
        {
            get
            {
                if (!Parameters.IsEmpty && Parameters["ModsToInstall"] != null)
                {
                    return (List<IInstallableElement>)Parameters["ModsToInstall"];
                }

                return null;
            }
        }

        protected override async Task OnLoadedAsync()
        {
            try
            {
                try
                {
                    var Instance = ServiceSingleton.Instances.WorkingInstance;

                    ServiceSingleton.Dashboard.Title("Nolvus Dashboard - [Instance Auto Installer]");
                    ServiceSingleton.Dashboard.Status(string.Format(
                        "{0} {1} - {2} (v{3})", 
                        Instance.Status.InstallStatus == InstanceInstallStatus.Installing ? "Installing" : "Updating", 
                        Instance.Name, 
                        Instance.Performance.Variant, 
                        ServiceSingleton.Packages.LoadedVersion));

                    ServiceSingleton.Dashboard.AdditionalSecondaryInfo("Error(s) : 0");

                    GlobalProgress();

                    ServiceSingleton.Files.RemoveDirectory(ServiceSingleton.Folders.NexusCacheDirectory, false);

                    await ServiceSingleton.Packages.InstallModList(ModsToInstall, new ModInstallSettings()
                    {
                        OnStartInstalling = () =>
                        {
                            Refresh(ServiceSingleton.Settings.RefreshInterval);
                        },
                        OnModInstalled = (Mod) =>
                        {
                            GlobalProgress();
                            ServiceSingleton.Logger.Log(string.Format("Mod : {0} installed.", Mod.Name));
                        },
                        OnModError = (ErrorCount) =>
                        {
                            if (ServiceSingleton.Packages.ErrorHandler.ThresholdEnabled)
                            {
                                ServiceSingleton.Dashboard.AdditionalSecondaryInfo(
                                    string.Format("Error(s) : {0} Threshold : {1} {2}",
                                    ServiceSingleton.Packages.ErrorHandler.ErrorsCount,
                                    ServiceSingleton.Settings.ErrorsThreshold,
                                    "(Errors will be displayed at the end of the installation)"));
                            }
                            else
                            {
                                ServiceSingleton.Dashboard.AdditionalSecondaryInfo(
                                    string.Format("Error(s) : {0} {1}",
                                    ServiceSingleton.Packages.ErrorHandler.ErrorsCount,
                                    "(Errors will be displayed at the end of the installation)"));
                            }
                        },
                        OnMaxErrors = () =>
                        {
                            ServiceSingleton.Dashboard.AdditionalSecondaryInfo(
                                string.Format("Error(s) : {0} {1}",
                                ServiceSingleton.Packages.ErrorHandler.ErrorsCount,
                                "(Maximum errors threshold reached, waiting for current queue to finish...)"));
                        },
                        Browser = () =>
                        {
                            var win = new BrowserWindow("about:blank");
                            win.Show();
                            return win.Engine;
                        }
                    });
                    _refreshCts?.Cancel();
                    await ServiceSingleton.Dashboard.LoadFrameAsync<LoadOrderFrame>(new FrameParameters(new FrameParameter(){Key = "Mode", Value = "Install"}));
                }
                catch
                {
                    _refreshCts?.Cancel();
                    ServiceSingleton.Dashboard.ClearInfo();
                    ServiceSingleton.Dashboard.LoadFrame<ErrorSummaryFrame>();
                }
            }
            finally
            {
                ServiceSingleton.Dashboard.NoStatus();
                ServiceSingleton.Dashboard.ProgressCompleted();  
            }
        }

        private void GlobalProgress()
        {
            ServiceSingleton.Dashboard.Info(
                string.Format("Installing mods ({0}%)", ServiceSingleton.Packages.InstallProgression));

            ServiceSingleton.Dashboard.AdditionalInfo(
                string.Format("Mods {0}/{1}",
                ServiceSingleton.Instances.WorkingInstance.Status.InstalledMods.Count,
                ModsToInstall == null ? ServiceSingleton.Packages.ModsCount : ModsToInstall.Count));
        }

        private void RefreshBox()
        {
            var queue = ServiceSingleton.Packages.ProgressQueue;

            ModsBox.Items.Clear();

            foreach (var item in queue.ToList())
                ModsBox.Items.Add(item);

            try
            {
                ServiceSingleton.Dashboard.AdditionalTertiaryInfo(
                    $"Download : {queue.DownloadSpeed:0.0}MB/s");
            }
            catch
            {
                // ignore if DownloadSpeed not available yet
            }
        }

        private void Refresh(int ms)
        {
            _refreshCts?.Cancel();
            _refreshCts = new CancellationTokenSource();
            var token = _refreshCts.Token;

            Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(RefreshBox);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(ms, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }

                    if (token.IsCancellationRequested)
                        break;

                    await Dispatcher.UIThread.InvokeAsync(RefreshBox);
                }

            }, token);
        }
    }
}
