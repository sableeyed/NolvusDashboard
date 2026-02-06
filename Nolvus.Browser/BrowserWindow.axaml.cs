using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Nolvus.Browser.Core;
using Nolvus.Core.Events;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace Nolvus.Browser
{
    public partial class BrowserWindow : Window, IBrowserInstance
    {
        private readonly AvaloniaCefBrowser _cef;
        private string _initialUrl;
        private volatile bool _canClose;
        private int _closeRequested;
        private readonly TaskCompletionSource<object?> _closedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public event OnBrowserClosedHandler? OnBrowserClosed;

        public BrowserWindow(string initialUrl = "about:blank")
        {
            InitializeComponent();

            _initialUrl = initialUrl;

            TitleBar.Title = "Nolvus Browser";
            TitleBar.CloseRequested += (_, __) => CloseBrowser();
            TitleBar.PointerPressed += OnTitleBarPointerPressed;

            _cef = new AvaloniaCefBrowser();
            BrowserHost.Children.Add(_cef);

            _cef.TitleChanged += (_, title) =>
                Dispatcher.UIThread.Post(() => TitleBar.Title = title);

            Opened += OnOpened;

            Closing += (_, e) =>
            {
                if (!_canClose)
                {
                    e.Cancel = true;
                    CloseBrowser();
                    return;
                }

                if (Interlocked.CompareExchange(ref _closeRequested, 0, 0) == 0)
                {
                    e.Cancel = true;
                    CloseBrowser();
                }
            };

            Closed += (_, __) =>
            {
                try { DisposeCefIfNeeded(); } catch { }

                _closedTcs.TrySetResult(null);
                OnBrowserClosed?.Invoke(this, EventArgs.Empty);
            };
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_initialUrl) && _initialUrl != "about:blank")
                NavigateInternal(_initialUrl);
        }

        private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private Task WaitForClosedAsync() => _closedTcs.Task;

        private void NavigateInternal(string url)
        {
            _cef.Address = url;
        }

        public void Navigate(string link, string title = null)
        {
            _canClose = true;

            if (!string.IsNullOrWhiteSpace(title))
                TitleBar.Title = title;

            _initialUrl = link;
            Dispatcher.UIThread.Post(() => NavigateInternal(link));
        }

        public void CloseBrowser()
        {
            if (Interlocked.Exchange(ref _closeRequested, 1) != 0)
                return;

            _canClose = true;

            Dispatcher.UIThread.Post(() =>
            {
                DisposeCefIfNeeded();

                try { Close(); } catch { }
            });
        }

        public async Task NexusSSOAuthentication(string id, string slug)
        {
            _canClose = true;

            TitleBar.Title = "Nexus SSO Authentication";

            var startUrl = $"https://www.nexusmods.com/sso?id={id}&application={slug}";
            var tcs = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? sender, LoadEndEventArgs e)
            {
                if (!e.Frame.IsMain)
                    return;

                var url = e.Frame.Url ?? string.Empty;

                if (url.Contains("https://www.nexusmods.com/oauth/callback?", StringComparison.OrdinalIgnoreCase))
                {
                    Dispatcher.UIThread.Post(() => NavigateInternal(startUrl));
                    return;
                }

                if (url.Contains("https://www.nexusmods.com/SSOauthorised?", StringComparison.OrdinalIgnoreCase))
                    tcs.TrySetResult(null);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _cef.LoadEnd += Handler;
                NavigateInternal(startUrl);
            });

            try
            {
                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try { _cef.LoadEnd -= Handler; } catch { }
                });

                CloseBrowser();
                await WaitForClosedAsync().ConfigureAwait(false);
            }
        }

        public async Task AwaitUserDownload(string link, string fileName, DownloadProgressChangedHandler progress)
        {
            _canClose = true;

            var handler = new ChromeDownloaderHandler(downloadLinkOnly: false);
            if (progress != null)
                handler.DownloadProgressChanged += progress;

            var downloadTcs = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            void Completed(object? s, FileDownloadRequestEvent e) => downloadTcs.TrySetResult(null);

            handler.OnFileDownloadCompleted += Completed;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _cef.DownloadHandler = handler;
                NavigateInternal(link);
            });

            try
            {
                await downloadTcs.Task.ConfigureAwait(false);

                var downloadedPath = handler.LastDownloadedFilePath;
                if (string.IsNullOrWhiteSpace(downloadedPath))
                {
                    downloadedPath = Path.Combine(ServiceSingleton.Folders.DownloadDirectory, fileName);
                }

                await Dispatcher.UIThread.InvokeAsync(CloseBrowser);
                await WaitForClosedAsync().ConfigureAwait(false);
            }
            finally
            {
                handler.OnFileDownloadCompleted -= Completed;

                if (progress != null)
                    handler.DownloadProgressChanged -= progress;
            }
        }

        public async Task<string> GetNexusManualDownloadLink(string modName, string link, string nexusModId)
        {
            _canClose = true;

            TitleBar.Title = $"Manual download [{modName}]";

            var handler = new LinkOnlyDownloadHandler();

            var tcs = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            void Requested(object? s, FileDownloadRequestEvent e)
            {
                if (!string.IsNullOrWhiteSpace(e.DownloadUrl))
                    tcs.TrySetResult(e.DownloadUrl);
            }

            handler.OnFileDownloadRequest += Requested;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _cef.DownloadHandler = handler;
                NavigateInternal(link);
            });

            string result;
            try
            {
                result = await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                handler.OnFileDownloadRequest -= Requested;

                await Dispatcher.UIThread.InvokeAsync(CloseBrowser);
                await WaitForClosedAsync().ConfigureAwait(false);
            }

            return result;
        }

        private void DisposeCefIfNeeded()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(DisposeCefIfNeeded);
                return;
            }

            try 
            {
                BrowserHost.Children.Remove(_cef);
            } 
            catch { }

            try 
            {
                _cef.Dispose();
            }
            catch { }
        }

        private sealed class LinkOnlyDownloadHandler : Xilium.CefGlue.Common.Handlers.DownloadHandler
        {
            public event OnFileDownloadRequestedHandler? OnFileDownloadRequest;

            protected override void OnBeforeDownload(Xilium.CefGlue.CefBrowser browser, Xilium.CefGlue.CefDownloadItem downloadItem, string name, Xilium.CefGlue.CefBeforeDownloadCallback callback)
            {
                OnFileDownloadRequest?.Invoke(this, new FileDownloadRequestEvent(downloadItem.Url));
            }

            protected override void OnDownloadUpdated(Xilium.CefGlue.CefBrowser browser, Xilium.CefGlue.CefDownloadItem downloadItem, Xilium.CefGlue.CefDownloadItemCallback callback)
            {
            }
        }

        private void ShowLoading()
        {
            LoadingOverlay.IsVisible = true;
            BrowserHost.IsVisible = false;
        }

        private void HideLoading()
        {
            LoadingOverlay.IsVisible = false;
            BrowserHost.IsVisible = true;
        }
    }
}
