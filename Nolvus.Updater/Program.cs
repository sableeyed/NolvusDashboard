using Avalonia;

namespace Nolvus.Updater;

internal static class Program
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<UpdaterApp>()
                     .UsePlatformDetect()
                     .LogToTrace();

    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
