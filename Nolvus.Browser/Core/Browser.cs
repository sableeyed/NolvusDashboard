using System.Runtime.CompilerServices;
using Nolvus.Core.Enums;
using Nolvus.Core.Events;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

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
        public string Url { get { return _url; } }

        private TaskCompletionSource<string>? _manualLinkTcs;
        private TaskCompletionSource<bool>? _downloadTcs;
        private TaskCompletionSource<bool>? _ssoTcs;
        public event OnBrowserClosedHandler? OnBrowserClosed;
        public event Action? HideLoadingRequested;
        public event Action<string>? PageInfoChanged;
        public event Action<string>? NavigationRequested;
        public event OnFileDownloadRequestedHandler? OnFileDownloadRequest;
        public event OnFileDownloadRequestedHandler? OnFileDownloadCompleted;
        //GUI MODE
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

        //HEADLESS MODE
        public Browser()
        {
            _browser = new AvaloniaCefBrowser();
            _downloadHandler = new ChromeDownloaderHandler(false);
            _browser.DownloadHandler = _downloadHandler;

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

            // Notify UI that navigation has begun (optional)
            PageInfoChanged?.Invoke(url);

            _browser!.Address = url;
        }
        
        //IDK ABOUT THIS
        public async Task AwaitUserDownload(string link, string fileName, DownloadProgressChangedHandler progress)
        {
            _downloadTcs = new TaskCompletionSource<bool>();

            if (progress != null)
                _downloadHandler.DownloadProgressChanged += progress;

            _browser!.Address = link;

            await WaitForMainFrameLoad();

            int loginNeeded = await EvaluateJsInt(ScriptManager.GetIsLoginNeeded());
            if (loginNeeded == 1)
            {
                _browser.ExecuteJavaScript(ScriptManager.GetRedirectToLogin());
                return;
            }

            int isAvailable = await EvaluateJsInt(ScriptManager.GetIsDownloadAvailable());
            if (isAvailable == 1)
            {
                _browser.ExecuteJavaScript(ScriptManager.GetNexusManualDownload());
            }
            else
            {
                throw new Exception("Slow download button not found.");
            }

            await _downloadTcs.Task;

            if (progress != null)
                _downloadHandler.DownloadProgressChanged -= progress;
        }


        //STUB
        public async Task<string> GetNexusManualDownloadLink(string ModName, string Link, string NexusModId)
        {
            // _manualLinkTcs = new TaskCompletionSource<string>();

            // _downloadHandler.SetLinkyOnly(true);

            // void OnLinkRequested(object? sender, FileDownloadRequestEvent e)
            // {
            //     _manualLinkTcs.TrySetResult(e.DownloadUrl);
            // }
            return "Unimplemented";
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


        public void CloseBrowser()
        {
            return;
        }

        private void HandleDownloadCompleted(object? sender, FileDownloadRequestEvent e)
        {
            //Forward to Browser/UI?
            OnFileDownloadCompleted?.Invoke(this, e);
            _downloadTcs?.TrySetResult(true);
        }

        private void HandleDownloadRequest(object? sender, FileDownloadRequestEvent e)
        {
            OnFileDownloadRequest?.Invoke(this, e);
            _downloadTcs?.TrySetResult(true);
        }

        private void Browser_LoadStart(object? sender, LoadStartEventArgs e)
        {
            return;
        }

        private void Browser_LoadEnd(object? sender, LoadEndEventArgs e)
        {
            if (!e.Frame.IsMain)
                return;

            var url = e.Frame.Url;
            PageInfoChanged?.Invoke(url);
        }

        //IDK ABOUT THIS
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
            var result = await _browser.EvaluateJavaScript<int>(script);
            return result;
        }



    }
}
