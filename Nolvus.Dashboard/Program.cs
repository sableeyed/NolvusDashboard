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
using Nolvus.Instance.Services;
using System.Net.Security;
using System.Security.Authentication;
using Vcc.Nolvus.Api.Installer.Core;
using System.Diagnostics;
using System.Reflection;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;
using System.Text;
using Nolvus.Dashboard.Services.Wine;
using QuestPDF.Drawing;
using QuestPDF.Fluent;

namespace Nolvus.Dashboard;

internal static class Program
{

    private static string? _cefCachePath;

    public static bool DebugMode { get; private set; }
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<DashboardApp>()
                     .UsePlatformDetect()
                     .LogToTrace()
                     .AfterSetup(_ => InitializeCef());

    private static void InitializeCef()
    {
        if (_cefCachePath != null)
            return;

        _cefCachePath = Path.Combine(Path.GetTempPath(), "Nolvus_Cef");

        AppDomain.CurrentDomain.ProcessExit += (_, __) => CleanupCef();

        var settings = new CefSettings
        {
            RootCachePath = _cefCachePath,
            WindowlessRenderingEnabled = false,
            MultiThreadedMessageLoop = true,
            NoSandbox = true
        };

        CefRuntimeLoader.Initialize(settings);
    }

    private static void CleanupCef()
    {
        try
        {
            CefRuntime.Shutdown();
        }
        catch { }

        if (string.IsNullOrEmpty(_cefCachePath))
            return;

        try
        {
            var dirInfo = new DirectoryInfo(_cefCachePath);
            if (dirInfo.Exists)
            {
                dirInfo.Delete(true);
            }
        }
        catch { }
    }


    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception;
        ServiceSingleton.Logger.Log(ex.Message + Environment.NewLine + "Stack =>" + ex.StackTrace);
    }

    private static void Loader(object sender, AssemblyLoadEventArgs args)
    {
        ServiceSingleton.Logger.Log("Assembly Loader : ==>" + args.LoadedAssembly.FullName);
    }

    private static Assembly Resolver(object sender, ResolveEventArgs args)
    {
            ServiceSingleton.Logger.Log("Assembly loader : Unable to load assembly ==> " + args.Name);
            return null;
    }
    [STAThread]
    public static void Main(string[] args)
    {

        if (!File.Exists("/usr/bin/7z") || (!File.Exists("/usr/bin/xdelta3")))
        {
            Console.WriteLine("7zip and xdelta3 are required to run this program. Please install the relevant packages");
            Environment.Exit(-1);
        }

        if (!File.Exists("/usr/bin/wine"))
        {
            Console.WriteLine("Wine is required and expected to be accessible at /usr/bin/wine");
            Environment.Exit(-1);
        }

        DebugMode = args.Contains("--debugging");

        var current = Process.GetCurrentProcess();
        var running = Process.GetProcessesByName(current.ProcessName);

        if (running.Length > 1)
        {
            Console.WriteLine("Another instance is already running");
            Environment.Exit(0);
        }

        var libDir = Path.Combine(AppContext.BaseDirectory, "lib");

        if (!Directory.Exists(libDir))
        {
            Directory.CreateDirectory(libDir);
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


        ServiceSingleton.RegisterService<ILogService>(new LogService());
        ServiceSingleton.Logger.LineBreak();
        ServiceSingleton.Logger.Log("***Nolvus Dashboard Initialization***");
        ServiceSingleton.Logger.Log("Starting new session : " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
        ServiceSingleton.Logger.Log("Architecture : " + (Environment.Is64BitProcess ? "x64" : "x86"));

        var PackageService = new PackageService();

        ServiceSingleton.RegisterService<IGlobalsService>(new GlobalsService());
        ServiceSingleton.RegisterService<ISettingsService>(new SettingsService());
        ServiceSingleton.RegisterService<IFolderService>(new FolderService());            
        ServiceSingleton.RegisterService<IInstanceService>(new InstanceService());
        ServiceSingleton.RegisterService<IUpdaterService>(new UpdaterService());
        ServiceSingleton.RegisterService<IPackageService>(PackageService);            
        ServiceSingleton.RegisterService<ILibService>(new LibService());
        ServiceSingleton.RegisterService<IGameService>(new GameService());
        ServiceSingleton.RegisterService<IFileService>(new FileService());
        ServiceSingleton.RegisterService<ISoftwareProvider>(PackageService);
        ServiceSingleton.RegisterService<IReportService>(new ReportService());
        ServiceSingleton.RegisterService<ICheckerService>(new CheckerService());
        ServiceSingleton.RegisterService<IWineRunner>(new WineRunner());

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPDF.Settings.FontDiscoveryPaths.Clear();
        QuestPDF.Settings.FontDiscoveryPaths.Add(Path.Combine("/usr/share/fonts"));

        AppDomain.CurrentDomain.AssemblyResolve += Resolver;
        AppDomain.CurrentDomain.AssemblyLoad += Loader;
        AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

}
