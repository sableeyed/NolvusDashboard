using System.Runtime.CompilerServices;
using Nolvus.Core.Events;
using Nolvus.Core.Interfaces;
using Xilium.CefGlue.Avalonia;
using Xilium.CefGlue.Common.Events;

namespace Nolvus.Browser.Core
{
    public class Browser //: BrowserInstance
    {

        private readonly AvaloniaCefBrowser? _browser;
        private readonly ChromeDownloaderHandler _downloadHandler;

        private TaskCompletionSource<string>? _manualLinkTcs;
        private TaskCompletionSource<bool>? _downloadTcs;
        private TaskCompletionSource<bool>? _ssoTcs;
        public event OnBrowserClosedHandler? OnBrowserClosed;

        public Browser(AvaloniaCefBrowser chromeBrowser)
        {
            _browser = chromeBrowser;

            _downloadHandler = new ChromeDownloaderHandler(false);
            _browser.DownloadHandler = _downloadHandler;
            _downloadHandler.OnFileDownloadCompleted += OnFileDownloadCompleted;
           // _downloadHandler.OnFileDownloadRequest += OnFileDownloadRequest;
        }

        public void Navigate(string url, string? title = null)
        {
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

        void CloseBrowser()
        {
            
        }

        //STUB
        // event OnBrowserClosedHandler OnBrowserClosed
        // {
            
        // }

        private void OnFileDownloadCompleted(object? sender, FileDownloadRequestEvent e)
        {
            _downloadTcs?.TrySetResult(true);
        }

    }
}
