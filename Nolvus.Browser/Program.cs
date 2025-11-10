using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;

namespace Nolvus.Browser
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var cachePath = Path.Combine(Path.GetTempPath(), "NolvusBrowser_" + Guid.NewGuid().ToString("N"));
            AppDomain.CurrentDomain.ProcessExit += (_, __) => Cleanup(cachePath);

            BuildAvaloniaApp(cachePath)
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnLastWindowClose);

            return 0;
        }

        private static AppBuilder BuildAvaloniaApp(string cachePath)
        {
            var settings = new CefSettings
            {
                RootCachePath = cachePath,
                WindowlessRenderingEnabled = false,
            };

            CefRuntimeLoader.Initialize(settings);

            return AppBuilder
                .Configure<RootApp>()
                .UsePlatformDetect();
        }

        private static void Cleanup(string cachePath)
        {
            try { CefRuntime.Shutdown(); } catch { }

            try
            {
                var di = new DirectoryInfo(cachePath);
                if (di.Exists) di.Delete(true);
            }
            catch { }
        }
    }

    public sealed class RootApp : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            {
                d.MainWindow = new BrowserWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
