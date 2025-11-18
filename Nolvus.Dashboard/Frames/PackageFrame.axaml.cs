using Nolvus.Core.Frames;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Vcc.Nolvus.Api.Installer.Services;
using Vcc.Nolvus.Api.Installer.Library;
using Nolvus.Core.Enums;
using Nolvus.Dashboard.Frames.Installer;
//using Nolvus.Dashboard.Frames.Instance;

namespace Nolvus.Dashboard.Frames
{
    public partial class PackageFrame : DashboardFrame
    {
        public PackageFrame(IDashboard Dashboard, FrameParameters Params) :base(Dashboard, Params)
        {
            InitializeComponent();
        }

        protected override async Task OnLoadedAsync()
        {
            ServiceSingleton.Dashboard.Info("Package loading...");

            try
            {
                try
                {
                    INolvusInstance Instance = ServiceSingleton.Instances.WorkingInstance;

                    switch (Instance.Status.InstallStatus)
                    {
                        case InstanceInstallStatus.None:                            
                            await Install(Instance);
                            break;

                        case InstanceInstallStatus.Installing:                            
                            //await Resume(Instance);
                            break;

                        case InstanceInstallStatus.Updating:                            
                            //await Update(Instance);
                            break;

                        default:
                            //await View(Instance);
                            break;

                    }                  
                }
                catch (Exception ex)
                {
                    await ServiceSingleton.Dashboard.Error("Error during package loading", ex.Message);
                }
            }
            finally
            {
                ServiceSingleton.Dashboard.NoStatus();
                ServiceSingleton.Dashboard.ProgressCompleted();
            }
        }

        protected async Task Install(INolvusInstance Instance)
        {
            IInstallPackageDTO Package = await ApiManager.Service.Installer.GetLatestPackage(Instance.Id);

            Instance.Version = Package.Version;

            await ServiceSingleton.Packages.Load(Package, (s, p) =>
            {
                ServiceSingleton.Dashboard.Status(string.Format("0 ({1}%)", s, p));
                ServiceSingleton.Dashboard.Progress(p);
            });

            ServiceSingleton.Logger.Log(string.Format("Start installing {0} -v {1}...", Instance.Name, Instance.Version));

            await ServiceSingleton.Dashboard.LoadFrameAsync<StockGameFrame>();
        }

        // protected async Task Update(INolvusInstance Instance)
        // {
        //     var Packages = await ApiManager.Service.Installer.GetLatestPackages(Instance.Id, Instance.Version);

        //     await ServiceSingleton.Packages.Merge(Packages, (s, p) =>
        //     {
        //         ServiceSingleton.Dashboard.Status(string.Format("{0} ({1}%)", s, p));
        //         ServiceSingleton.Dashboard.Progress(p);
        //     });

        //     ServiceSingleton.Logger.Log(string.Format("Updating {0} - v {1} to v {2}...", Instance.Name, Instance.Version, Packages.Last().Version));

        //     await ServiceSingleton.Dashboard.LoadFrameAsync<InstallFrame>();
        // }

        // protected async Task View(INolvusInstance Instance)
        // {
        //     await ServiceSingleton.Packages.Load(await ApiManager.Service.Installer.GetPackage(Instance.Id, Instance.Version), (s, p) =>
        //     {
        //         ServiceSingleton.Dashboard.Status(string.Format("{0} ({1}%)", s, p));
        //         ServiceSingleton.Dashboard.Progress(p);
        //     });

        //     ServiceSingleton.Logger.Log(string.Format("Viewing {0} - v {1}...", Instance.Name, Instance.Version));

        //     ServiceSingleton.Dashboard.LoadFrame<InstanceDetailFrame>();
        // }

        // protected async Task Resume(INolvusInstance Instance)
        // {
        //     await ServiceSingleton.Packages.Load(await ApiManager.Service.Installer.GetPackage(Instance.Id, Instance.Version), (s, p) =>
        //     {
        //         ServiceSingleton.Dashboard.Status(string.Format("{0} ({1}%)", s, p));
        //         ServiceSingleton.Dashboard.Progress(p);
        //     });

        //     ServiceSingleton.Logger.Log(string.Format("Resume installing {0} - v {1}...", Instance.Name, Instance.Version));

        //     await ServiceSingleton.Dashboard.LoadFrameAsync<InstallFrame>(); 
        // }
    }
}