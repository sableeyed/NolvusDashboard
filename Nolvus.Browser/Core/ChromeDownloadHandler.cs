using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Nolvus.Core.Events;
using Nolvus.Core.Services;
using Xilium.CefGlue;
using Xilium.CefGlue.Common.Handlers;

namespace Nolvus.Browser.Core
{
    public class ChromeDownloaderHandler : DownloadHandler
    {
        private bool _isDownloadComplete;
        private bool _linkOnly;
        private string? _currentDownloadPath;
        public bool IsDownloadComplete => _isDownloadComplete;
        public string? LastDownloadedFilePath { get; private set; }
        private readonly Stopwatch _sw = new();
        private readonly DownloadProgress _progress = new();
        public event DownloadProgressChangedHandler? DownloadProgressChanged;
        public event OnFileDownloadRequestedHandler? OnFileDownloadRequest;
        public event OnFileDownloadRequestedHandler? OnFileDownloadCompleted;
        public event EventHandler<CefDownloadItem>? OnBeforeDownloadFired;
        public event EventHandler<CefDownloadItem>? OnDownloadUpdatedFired;

        public ChromeDownloaderHandler(bool downloadLinkOnly, DownloadProgressChangedHandler? onProgress = null)
        {
            if (onProgress != null)
                DownloadProgressChanged += onProgress;

            _linkOnly = downloadLinkOnly;
        }

        public void SetLinkOnly(bool value) => _linkOnly = value;

        protected override void OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string suggestedName, CefBeforeDownloadCallback callback)
        {
            OnBeforeDownloadFired?.Invoke(this, downloadItem);

            _isDownloadComplete = false;
            LastDownloadedFilePath = null;
            _currentDownloadPath = null;

            _sw.Reset();
            _sw.Start();

            if (callback == null)
                return;

            string downloads = ServiceSingleton.Folders.DownloadDirectory;

            string fileName = downloadItem.SuggestedFileName;

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = suggestedName;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = Path.GetFileName(new Uri(downloadItem.Url).LocalPath);
                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = "download.bin";
            }

            string fullPath = Path.Combine(downloads, fileName);

            LastDownloadedFilePath = fullPath;
            _currentDownloadPath = fullPath;

            OnFileDownloadRequest?.Invoke(this, new FileDownloadRequestEvent(downloadItem.Url));

            try
            {
                using (callback)
                {
                    callback.Continue(fullPath, showDialog: false);
                }
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[CEF] OnBeforeDownload Continue failed: {ex.Message}");
            }
        }


        protected override void OnDownloadUpdated(CefBrowser browser, CefDownloadItem downloadItem, CefDownloadItemCallback callback)
        {
            OnDownloadUpdatedFired?.Invoke(this, downloadItem);

            if (!downloadItem.IsValid)
                return;

            _progress.BytesReceived = downloadItem.ReceivedBytes;

            if (downloadItem.TotalBytes > 0L)
                _progress.TotalBytesToReceive = downloadItem.TotalBytes;

            _progress.ProgressPercentage = downloadItem.PercentComplete;

            var seconds = _sw.Elapsed.TotalSeconds;
            _progress.Speed = seconds > 0.001 ? downloadItem.ReceivedBytes / 1024d / 1024d / seconds : 0;

            _progress.BytesReceivedAsString = (downloadItem.ReceivedBytes / 1024d / 1024d).ToString("0.00");
            _progress.TotalBytesToReceiveAsString = (downloadItem.TotalBytes / 1024d / 1024d).ToString("0.00");

            _progress.FileName = !string.IsNullOrWhiteSpace(downloadItem.SuggestedFileName) ? downloadItem.SuggestedFileName : (LastDownloadedFilePath != null ? Path.GetFileName(LastDownloadedFilePath) : string.Empty);

            if (downloadItem.IsInProgress && downloadItem.PercentComplete != 0)
                DownloadProgressChanged?.Invoke(this, _progress);

            if (downloadItem.IsComplete)
            {
                _sw.Stop();
                _isDownloadComplete = true;
            }
        }
    }
}
