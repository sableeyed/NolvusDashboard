using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Nolvus.Core.Events;
using Nolvus.Core.Enums;
using Xilium.CefGlue.Avalonia;

namespace Nolvus.Browser.Core
{
    public sealed class BrowserSession : IAsyncDisposable, IBrowserInstance
    {
        private BrowserWindow? _window;
        private AvaloniaCefBrowser? _browser;
        private WebSite _website;
        private string? _currentFile;
        private ChromeDownloaderHandler? _downloadHandler;
        private TaskCompletionSource<bool>? _downloadTcs;
        private TaskCompletionSource<bool>? _ssoTcs;
        private TaskCompletionSource<object?>? _sessionTcs;
        private int _ended; // 0 = running, 1 = ended
        private string? _expectedFile;
        public event EventHandler? SessionEnded;
        public event OnBrowserClosedHandler? OnBrowserClosed;

        public async Task RunAsync(string url, CancellationToken cancellationToken = default)
        {
            if (_window != null)
                throw new InvalidOperationException("BrowserSession is already running.");

            _sessionTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _window = new BrowserWindow();
                _browser = _window.Browser;

                _downloadHandler = new ChromeDownloaderHandler(false);
                _browser.DownloadHandler = _downloadHandler;

                _downloadHandler.OnFileDownloadCompleted += HandleDownloadCompleted;

                _browser.LoadEnd += (_, e) =>
                {
                    if (!e.Frame.IsMain)
                        return;

                    if (_website == WebSite.EnbDev)
                        HandleEnbDev();
                };

                _window.Closed += OnWindowClosed;
                _window.Show();
                Navigate(url);
            });

            using (cancellationToken.Register(() => _sessionTcs.TrySetCanceled(cancellationToken)))
            {
                await _sessionTcs.Task;
            }

            SessionEnded?.Invoke(this, EventArgs.Empty);
        }

        public async Task AwaitUserDownload(string link, string fileName, DownloadProgressChangedHandler progress)
        {
            if (_browser == null || _downloadHandler == null)
                throw new InvalidOperationException("BrowserSession not initialized.");

            _expectedFile = fileName;
            _currentFile = fileName;

            _downloadTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (progress != null)
                _downloadHandler.DownloadProgressChanged += progress;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Navigate(link);
            });

            await _downloadTcs.Task;

            if (progress != null)
                _downloadHandler.DownloadProgressChanged -= progress;

            await EnsureFileFullyWritten(Path.Combine(ServiceSingleton.Folders.DownloadDirectory, fileName));
        }

        private void HandleDownloadCompleted(object? sender, FileDownloadRequestEvent e)
        {
            _downloadTcs?.TrySetResult(true);
            OnBrowserClosed?.Invoke(this, EventArgs.Empty);

            Dispatcher.UIThread.Post(() => _window?.Close());
        }

        private void DetectWebsite(string url)
        {
            if (url.Contains("www.nexusmods.com/sso"))
                _website = WebSite.NexusSSO;
            else if (url.Contains("nexusmods.com"))
                _website = WebSite.Nexus;
            else if (url.Contains("enbdev.com"))
                _website = WebSite.EnbDev;
            else
                _website = WebSite.Other;
        }

        public void Navigate(string url, string? title = null)
        {
            if (_browser == null)
                throw new InvalidOperationException("Browser not initialized.");

            DetectWebsite(url);
            _browser.Address = url;
        }

        private void ExecuteJs(string script)
        {
            _browser?.ExecuteJavaScript(script);
        }

        private void HandleEnbDev()
        {
            if (string.IsNullOrEmpty(_currentFile))
                return;

            var script = ScriptManager.GetHandleENBDev(_currentFile);
            ExecuteJs(script);
        }

        public async Task CloseAsync()
        {
            if (Interlocked.Exchange(ref _ended, 1) != 0)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_window != null)
                {
                    _window.Closed -= OnWindowClosed;
                    _window.Close();
                    _window = null;
                    _browser = null;
                }
            });

            _downloadTcs?.TrySetResult(true);
            _sessionTcs?.TrySetResult(null);
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _ended, 1) != 0)
                return;

            _downloadTcs?.TrySetResult(true);
            _sessionTcs?.TrySetResult(null);
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
        }

        private static async Task EnsureFileFullyWritten(string filePath)
        {
            const int stableChecks = 3;
            long lastSize = -1;
            int stableCount = 0;

            while (true)
            {
                if (!File.Exists(filePath))
                {
                    await Task.Delay(100);
                    continue;
                }

                long size = new FileInfo(filePath).Length;

                if (size == lastSize)
                {
                    stableCount++;
                    if (stableCount >= stableChecks)
                        break;
                }
                else
                {
                    stableCount = 0;
                    lastSize = size;
                }

                await Task.Delay(150);
            }

            await Task.Delay(150);
        }

        public void Complete()
        {
            _downloadTcs?.TrySetResult(true);
            _ssoTcs?.TrySetResult(true);
            _sessionTcs?.TrySetResult(null);

            Dispatcher.UIThread.Post(() =>
            {
                _window?.Close();
            });
        }

    }
}
