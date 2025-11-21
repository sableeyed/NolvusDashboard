using System.Runtime.CompilerServices;
using Nolvus.Core.Enums;
using Nolvus.Core.Events;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;
using System.IO;
using System.Threading.Tasks;

namespace Nolvus.Browser.Core
{
    public class Browser : IBrowserInstance
    {
        private readonly AvaloniaCefBrowser? _browser;
        private readonly ChromeDownloaderHandler _downloadHandler;
        private WebSite website;
        private string file;
        private string modId;
        private string _url;
        public string Url => _url;

        private TaskCompletionSource<bool>? _downloadTcs;

        public event OnBrowserClosedHandler? OnBrowserClosed;
        public event Action? HideLoadingRequested;
        public event Action<string>? PageInfoChanged;
        public event Action<string>? NavigationRequested;
        public event OnFileDownloadRequestedHandler? OnFileDownloadRequest;
        public event OnFileDownloadRequestedHandler? OnFileDownloadCompleted;

        public Browser(AvaloniaCefBrowser chromeBrowser)
        {
            _browser = chromeBrowser;

            _downloadHandler = new ChromeDownloaderHandler(false);
            _browser.DownloadHandler = _downloadHandler;

            _downloadHandler.OnFileDownloadCompleted += HandleDownloadCompleted;
            _downloadHandler.OnFileDownloadRequest += HandleDownloadRequest;

            _browser.LoadStart += Browser_LoadStart;
            _browser.LoadEnd += Browser_LoadEnd;
        }

        public void Navigate(string url, string? title = null)
        {
            _url = url;

            if (url.Contains("www.nexusmods.com/sso"))
                website = WebSite.NexusSSO;
            else if (url.Contains("nexusmods.com"))
                website = WebSite.Nexus;
            else if (url.Contains("enbdev.com"))
                website = WebSite.EnbDev;
            else
                website = WebSite.Other;

            PageInfoChanged?.Invoke(url);
            _browser!.Address = url;
        }

        public async Task AwaitUserDownload(string link, string fileName, DownloadProgressChangedHandler progress)
        {
            file = fileName;
            _downloadTcs = new TaskCompletionSource<bool>();

            if (progress != null)
                _downloadHandler.DownloadProgressChanged += progress;

            Navigate(link);

            await WaitForMainFrameLoad();

            if (website == WebSite.EnbDev)
            {
                HandleEnbDev();
            }

            await _downloadTcs.Task;

            await EnsureFileFullyWritten(Path.Combine(
                ServiceSingleton.Folders.DownloadDirectory,
                fileName
            ));

            if (progress != null)
                _downloadHandler.DownloadProgressChanged -= progress;
        }

        public Task<string> GetNexusManualDownloadLink(string ModName, string Link, string NexusModId)
        {
            return Task.FromResult("Unimplemented");
        }

        public Task NexusSSOAuthentication(string id, string slug)
        {
            if (_browser != null)
            {
                var ssoUrl = $"https://www.nexusmods.com/sso?id={id}&application={slug}";
                Navigate(ssoUrl);
            }
            return Task.CompletedTask;
        }

        //Required by IBrowserInstance
        public void CloseBrowser()
        {
            OnBrowserClosed?.Invoke(this, EventArgs.Empty);
        }


        private void HandleDownloadCompleted(object? sender, FileDownloadRequestEvent e)
        {
            OnFileDownloadCompleted?.Invoke(this, e);
            _downloadTcs?.TrySetResult(true);
            OnBrowserClosed?.Invoke(this, EventArgs.Empty);
        }

        private void HandleDownloadRequest(object? sender, FileDownloadRequestEvent e)
        {
            OnFileDownloadRequest?.Invoke(this, e);
        }

        private void Browser_LoadStart(object? sender, LoadStartEventArgs e)
        {
            // no-op
        }

        private void Browser_LoadEnd(object? sender, LoadEndEventArgs e)
        {
            if (!e.Frame.IsMain)
                return;

            var url = e.Frame.Url;
            PageInfoChanged?.Invoke(url);

            if (website == WebSite.EnbDev)
            {
                HandleEnbDev();
            }
        }

        private Task WaitForMainFrameLoad()
        {
            var tcs = new TaskCompletionSource<bool>();

            void Handler(object? sender, LoadEndEventArgs e)
            {
                if (e.Frame.IsMain)
                {
                    _browser.LoadEnd -= Handler;
                    tcs.TrySetResult(true);
                }
            }

            _browser.LoadEnd += Handler;
            return tcs.Task;
        }

        private async Task<int> EvaluateJsInt(string script)
        {
            var result = await _browser!.EvaluateJavaScript<int>(script);
            return result;
        }

        private void HandleEnbDev()
        {
            if (string.IsNullOrEmpty(file))
                return;

            string script = ScriptManager.GetHandleENBDev(file);
            _browser!.ExecuteJavaScript(script);
        }

        private async Task EnsureFileFullyWritten(string filePath)
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
    }
}
