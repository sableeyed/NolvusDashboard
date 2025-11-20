using System;
using System.Threading;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace Nolvus.Browser
{
    /// <summary>
    /// True headless CefGlue browser used only to resolve real download URLs
    /// (e.g. ENB: click JS button -> capture download URL).
    /// It does NOT show any UI and does NOT download the file itself.
    /// </summary>
    public sealed class HeadlessBrowser : CefClient, IDisposable
    {
        private readonly TaskCompletionSource<CefBrowser> _browserCreatedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _pageLoadedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<string> _downloadUrlTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly HeadlessLoadHandler _loadHandler;
        private readonly HeadlessLifeSpanHandler _lifeSpanHandler;
        private readonly HeadlessDownloadHandler _downloadHandler;

        private CefBrowser? _browser;
        private bool _disposed;

        public HeadlessBrowser()
        {
            _loadHandler = new HeadlessLoadHandler(_pageLoadedTcs);
            _lifeSpanHandler = new HeadlessLifeSpanHandler(_browserCreatedTcs);
            _downloadHandler = new HeadlessDownloadHandler(_downloadUrlTcs);
        }

        #region CefClient overrides

        protected override CefLifeSpanHandler GetLifeSpanHandler() => _lifeSpanHandler;
        protected override CefLoadHandler GetLoadHandler() => _loadHandler;
        protected override CefDownloadHandler GetDownloadHandler() => _downloadHandler;

        #endregion

        /// <summary>
        /// Loads the given page URL, waits for it to finish loading, executes the provided JS,
        /// and waits until a file download is initiated. Returns the real file URL.
        /// </summary>
        /// <param name="pageUrl">Page URL (e.g. ENB download page).</param>
        /// <param name="javascriptToClickDownload">
        /// JavaScript that clicks the real download button (e.g. "document.querySelector('a[href*=\"download\"]').click();").
        /// </param>
        /// <param name="timeoutMs">Fails if no download is initiated in this time.</param>
        public async Task<string> ResolveDownloadUrlAsync(string pageUrl, string javascriptToClickDownload, int timeoutMs = 30000)
        {
            ThrowIfDisposed();

            // 1. Create windowless browser
            var wi = CefWindowInfo.Create();
            wi.SetAsWindowless(IntPtr.Zero, true);

            var browserSettings = new CefBrowserSettings();

            // NOTE: We assume CefRuntime.Initialize has already been called in your app.
            CefBrowserHost.CreateBrowser(wi, this, browserSettings, pageUrl);

            using var cts = new CancellationTokenSource(timeoutMs);

            // 2. Wait for browser instance
            using (cts.Token.Register(() => _browserCreatedTcs.TrySetCanceled()))
            {
                _browser = await _browserCreatedTcs.Task.ConfigureAwait(false);
            }

            // 3. Wait for main frame load to finish
            using (cts.Token.Register(() => _pageLoadedTcs.TrySetCanceled()))
            {
                await _pageLoadedTcs.Task.ConfigureAwait(false);
            }

            // 4. Execute the JS to click the download button
            _browser.GetMainFrame().ExecuteJavaScript(javascriptToClickDownload, pageUrl, 0);

            // 5. Wait for download URL to be captured
            using (cts.Token.Register(() => _downloadUrlTcs.TrySetCanceled()))
            {
                var downloadUrl = await _downloadUrlTcs.Task.ConfigureAwait(false);
                return downloadUrl;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                if (_browser != null)
                {
                    var host = _browser.GetHost();
                    host.CloseBrowser(true);
                    host.Dispose();
                    _browser.Dispose();
                    _browser = null;
                }
            }
            catch { }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HeadlessBrowser));
        }

        #region Nested handler classes

        /// <summary>
        /// Signals when the browser has been created.
        /// </summary>
        private sealed class HeadlessLifeSpanHandler : CefLifeSpanHandler
        {
            private readonly TaskCompletionSource<CefBrowser> _browserCreatedTcs;

            public HeadlessLifeSpanHandler(TaskCompletionSource<CefBrowser> browserCreatedTcs)
            {
                _browserCreatedTcs = browserCreatedTcs;
            }

            protected override void OnAfterCreated(CefBrowser browser)
            {
                base.OnAfterCreated(browser);
                _browserCreatedTcs.TrySetResult(browser);
            }
        }

        /// <summary>
        /// Signals when the main frame has finished loading.
        /// </summary>
        private sealed class HeadlessLoadHandler : CefLoadHandler
        {
            private readonly TaskCompletionSource<bool> _pageLoadedTcs;

            public HeadlessLoadHandler(TaskCompletionSource<bool> pageLoadedTcs)
            {
                _pageLoadedTcs = pageLoadedTcs;
            }

            protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
            {
                base.OnLoadEnd(browser, frame, httpStatusCode);

                if (frame.IsMain && !_pageLoadedTcs.Task.IsCompleted)
                {
                    _pageLoadedTcs.TrySetResult(true);
                }
            }
        }

        /// <summary>
        /// Captures the real file URL when a download starts.
        /// </summary>
        private sealed class HeadlessDownloadHandler : CefDownloadHandler
        {
            private readonly TaskCompletionSource<string> _downloadUrlTcs;

            public HeadlessDownloadHandler(TaskCompletionSource<string> downloadUrlTcs)
            {
                _downloadUrlTcs = downloadUrlTcs;
            }

            protected override void OnBeforeDownload(
                CefBrowser browser,
                CefDownloadItem downloadItem,
                string suggestedName,
                CefBeforeDownloadCallback callback)
            {
                // Capture the download URL and cancel the actual CEF download
                if (!_downloadUrlTcs.Task.IsCompleted && !string.IsNullOrEmpty(downloadItem.Url))
                {
                    _downloadUrlTcs.TrySetResult(downloadItem.Url);
                }

                // We don't want CEF to download to disk, so do not call Continue.
                callback.Dispose();
            }

            protected override void OnDownloadUpdated(CefBrowser browser, CefDownloadItem downloadItem, CefDownloadItemCallback callback)
            {
                // Not used for URL resolution; could be extended for progress.
                base.OnDownloadUpdated(browser, downloadItem, callback);
            }
        }

        #endregion
    }
}
