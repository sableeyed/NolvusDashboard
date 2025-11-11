using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace Nolvus.Browser
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<BrowserApplication>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}
