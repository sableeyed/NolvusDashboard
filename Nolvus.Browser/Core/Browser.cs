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
        
        //STUB
        public async Task AwaitUserDownload(string Link, string FileName, DownloadProgressChangedHandler Progress)
        {
            return;
            // _downloadTcs = new TaskCompletionSource<bool>();
            // if (Progress != null)
            // {
            //     _downloadHandler.DownloadProgressChanged += Progress;
            // }
            // try
            // {
            //     _browser!.Address = Link;
            //     await _downloadTcs.Task;
            // }
            // finally
            // {
            //     if (Progress != null)
            //     {
            //         _downloadHandler.DownloadProgressChanged -= Progress;
            //     }
            // }
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

        public async Task NexusSSOAuthentication(string Id, string Slug)
        {
            
        }

        public void CloseBrowser()
        {
            
        }

        //STUB
        // event OnBrowserClosedHandler OnBrowserClosed
        // {

        // }

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
            ServiceSingleton.Logger.Log("BROWSER LOAD START");
        }
        
        private void Browser_LoadEnd(object? sender, LoadEndEventArgs e)
        {
            ServiceSingleton.Logger.Log("BROWSER LOAD END");
        }
    }
}
