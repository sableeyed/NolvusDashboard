using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;

namespace Nolvus.Browser
{
    public static class Browser
    {
        private static bool _initialized = false;

        public static void InitCefIfNeeded(string cachePath)
        {
            if (_initialized)
                return;

            CefRuntimeLoader.Initialize(new CefSettings
            {
                RootCachePath = cachePath,
                WindowlessRenderingEnabled = false
            });

            _initialized = true;
        }

        public static BrowserWindow OpenBrowser(string startUrl = "https://www.google.com")
        {
            // Ensure Avalonia app is already running (dashboard controls this)
            var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime!;

            var wnd = new BrowserWindow();
            wnd.SetUrl(startUrl);
            wnd.Show();
            return wnd;
        }
    }
}
