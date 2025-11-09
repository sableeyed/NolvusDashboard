using Avalonia;
using System;

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
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
}
