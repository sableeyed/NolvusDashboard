using Avalonia;

namespace Nolvus.Updater;

internal static class Program
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Nolvus.Updater.App>()
                     .UsePlatformDetect()
                     .LogToTrace();

    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
