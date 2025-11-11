using Xilium.CefGlue;

namespace Nolvus.Browser
{
    public static class CefLifecycle
    {
        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            // Required by CEF to bind to the current process.
            var mainArgs = new CefMainArgs(System.Environment.GetCommandLineArgs());

            // Basic settings
            var settings = new CefSettings
            {
                MultiThreadedMessageLoop = true,
                NoSandbox = true,
                LogSeverity = CefLogSeverity.Debug,
            };

            CefRuntime.Load();
            CefRuntime.Initialize(mainArgs, settings, application: null, windowsSandboxInfo: default);

            _initialized = true;
        }

        public static void Release()
        {
            if (!_initialized)
                return;

            CefRuntime.Shutdown();
            _initialized = false;
        }
    }
}
