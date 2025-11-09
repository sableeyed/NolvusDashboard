using Avalonia;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Services.Logger;
using Nolvus.Services.Globals;
using Nolvus.Services.Settings;
using Nolvus.Services.Folders;
using Nolvus.Services.Updater;
using Nolvus.Services.Lib;
using Nolvus.Services.Game;
using Nolvus.Services.Files;
using Nolvus.Services.Checker;
using Nolvus.Package.Services;
using System.Net.Security;
using System.Security.Authentication;
using Nolvus.Api.Installer.Core;
using System.Diagnostics;

namespace Nolvus.Dashboard;

internal static class Program
{
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<DashboardApp>()
                     .UsePlatformDetect()
                     .LogToTrace();

    [STAThread]
    public static void Main(string[] args)
    {

        var current = Process.GetCurrentProcess();
        var running = Process.GetProcessesByName(current.ProcessName);

        if (running.Length > 1)
        {
            Console.WriteLine("Another instance is already running");
            Environment.Exit(0);
        }

        ServiceSingleton.RegisterService<ILogService>(new LogService());
        ServiceSingleton.Logger.LineBreak();
        ServiceSingleton.Logger.Log("***Nolvus Dashboard Initialization***");
        ServiceSingleton.Logger.Log("Starting new session : " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
        ServiceSingleton.Logger.Log("Architecture : " + (Environment.Is64BitProcess ? "x64" : "x86"));

        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            },
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            MaxConnectionsPerServer = 100
        };

        var http = new HttpClient(handler);

        var PackageService = new PackageService();

        ServiceSingleton.RegisterService<IGlobalsService>(new GlobalsService());
        ServiceSingleton.RegisterService<ISettingsService>(new SettingsService());
        ServiceSingleton.RegisterService<IFolderService>(new FolderService());            
        //ServiceSingleton.RegisterService<IInstanceService>(new InstanceService());
        ServiceSingleton.RegisterService<IUpdaterService>(new UpdaterService());
        ServiceSingleton.RegisterService<IPackageService>(PackageService);            
        ServiceSingleton.RegisterService<ILibService>(new LibService());
        ServiceSingleton.RegisterService<IGameService>(new GameService());
        ServiceSingleton.RegisterService<IFileService>(new FileService());
        ServiceSingleton.RegisterService<ISoftwareProvider>(PackageService);
        ServiceSingleton.RegisterService<IReportService>(new ReportService());
        ServiceSingleton.RegisterService<ICheckerService>(new CheckerService());

        // AppDomain.CurrentDomain.AssemblyResolve += Resolver;
        // AppDomain.CurrentDomain.AssemblyLoad += Loader;
        // AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
}
