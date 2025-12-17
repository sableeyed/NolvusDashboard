using System;
using System.IO;
using System.Diagnostics;
using Nolvus.Core.Events;
using Nolvus.Core.Services;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Handlers;

namespace Nolvus.Browser.Core
{
    public sealed class ChromeDownloaderHandler : DownloadHandler
    {
        private bool _isDownloadComplete;
        private bool _linkOnly;
        public bool IsDownloadComplete => _isDownloadComplete;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly DownloadProgress _downloadProgress = new DownloadProgress();
        public event DownloadProgressChangedHandler? DownloadProgressChanged;
        public event OnFileDownloadRequestedHandler? OnFileDownloadRequest;
        public event OnFileDownloadRequestedHandler? OnFileDownloadCompleted;
        public event EventHandler<CefDownloadItem>? OnBeforeDownloadFired;
        public event EventHandler<CefDownloadItem>? OnDownloadUpdatedFired;

        public ChromeDownloaderHandler(bool downloadLinkOnly, DownloadProgressChangedHandler? onProgress = null)
        {
            _linkOnly = downloadLinkOnly;

            if (onProgress != null)
                DownloadProgressChanged += onProgress;
        }

        public void SetLinkOnly(bool value)
        {
            _linkOnly = value;
        }

        protected override void OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string suggestedName, CefBeforeDownloadCallback callback)
        {
            OnBeforeDownloadFired?.Invoke(this, downloadItem);

            if (_linkOnly)
            {
                OnFileDownloadRequest?.Invoke(this, new FileDownloadRequestEvent(downloadItem.Url));
                return;
            }

            string downloadsDir = ServiceSingleton.Folders.DownloadDirectory;
            string fileName = downloadItem.SuggestedFileName;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = Path.GetFileName(new Uri(downloadItem.Url).LocalPath);

                if (string.IsNullOrWhiteSpace(fileName))
                    fileName = "download.bin";
            }

            string fullPath = Path.Combine(downloadsDir, fileName);

            _stopwatch.Restart();

            callback.Continue(fullPath, showDialog: false);

            OnFileDownloadRequest?.Invoke(this, new FileDownloadRequestEvent(downloadItem.Url));
        }

        protected override void OnDownloadUpdated(CefBrowser browser, CefDownloadItem downloadItem, CefDownloadItemCallback callback)
        {
            OnDownloadUpdatedFired?.Invoke(this, downloadItem);

            if (!downloadItem.IsValid)
                return;

            _downloadProgress.BytesReceived = downloadItem.ReceivedBytes;

            if (downloadItem.TotalBytes > 0)
                _downloadProgress.TotalBytesToReceive = downloadItem.TotalBytes;

            _downloadProgress.ProgressPercentage = downloadItem.PercentComplete;
            _downloadProgress.FileName = downloadItem.SuggestedFileName;

            if (_stopwatch.IsRunning && _stopwatch.Elapsed.TotalSeconds > 0)
            {
                _downloadProgress.Speed = downloadItem.ReceivedBytes / 1024d / 1024d / _stopwatch.Elapsed.TotalSeconds;
            }

            _downloadProgress.BytesReceivedAsString = (downloadItem.ReceivedBytes / 1024d / 1024d).ToString("0.00");
            _downloadProgress.TotalBytesToReceiveAsString = (downloadItem.TotalBytes / 1024d / 1024d).ToString("0.00");

            if (downloadItem.IsInProgress && downloadItem.PercentComplete > 0)
            {
                DownloadProgressChanged?.Invoke(this, _downloadProgress);
            }

            if (downloadItem.IsComplete)
            {
                _stopwatch.Stop();
                _isDownloadComplete = true;

                OnFileDownloadCompleted?.Invoke(this, new FileDownloadRequestEvent(downloadItem.Url));
            }
        }
    }
}
