using System.IO;
using System.Diagnostics;
using Nolvus.Core.Events;
using Nolvus.Core.Services;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;
using Xilium.CefGlue.Common.Handlers;

namespace Nolvus.Browser.Core {
    //or CefDownloadHandler?
	public class ChromeDownloaderHandler : DownloadHandler {

		private bool _IsDownloadComplete = false;
		private bool LinkOnly;

		public bool IsDownloadComplete {
			get {
				return _IsDownloadComplete;
			}
		}

        public void SetLinkyOnly(bool value)
        {
            LinkOnly = value;
        }

		private Stopwatch SW = new Stopwatch();

		private readonly DownloadProgress DownloadProgress;
		public event DownloadProgressChangedHandler? DownloadProgressChanged;

		public event OnFileDownloadRequestedHandler? OnFileDownloadRequest;
		public event OnFileDownloadRequestedHandler? OnFileDownloadCompleted;

		public event EventHandler<CefDownloadItem>? OnBeforeDownloadFired;
		public event EventHandler<CefDownloadItem>? OnDownloadUpdatedFired;


		public ChromeDownloaderHandler(bool DownloadLinkOnly, DownloadProgressChangedHandler OnProgress = null) {
			if(OnProgress != null) DownloadProgressChanged += OnProgress;
			LinkOnly = DownloadLinkOnly;
			DownloadProgress = new DownloadProgress();
		}

		protected override void OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string name, CefBeforeDownloadCallback callback) {
            OnBeforeDownloadFired?.Invoke(this, downloadItem);

            string downloads = ServiceSingleton.Folders.DownloadDirectory;
            string suggested = downloadItem.SuggestedFileName;

            // ENB returns no filename → user clicked HTML page not an archive
            if (string.IsNullOrWhiteSpace(suggested))
            {
                // Fallback to the URL's last segment OR a generic filename
                suggested = Path.GetFileName(new Uri(downloadItem.Url).LocalPath);

                if (string.IsNullOrWhiteSpace(suggested))
                    suggested = "download.bin"; // last fallback
            }

            string fullPath = Path.Combine(downloads, suggested);

            callback.Continue(fullPath, showDialog: false);
		}

		protected override void OnDownloadUpdated(CefBrowser browser, CefDownloadItem downloadItem, CefDownloadItemCallback callback) {
            OnDownloadUpdatedFired?.Invoke(this, downloadItem);

            if(downloadItem.IsValid) {
                DownloadProgress.BytesReceived = downloadItem.ReceivedBytes;

                if (downloadItem.TotalBytes > 0L)
                    DownloadProgress.TotalBytesToReceive = downloadItem.TotalBytes;

                DownloadProgress.ProgressPercentage = downloadItem.PercentComplete;

                DownloadProgress.Speed = downloadItem.ReceivedBytes / 1024d / 1024d / SW.Elapsed.TotalSeconds;

                DownloadProgress.BytesReceivedAsString = (downloadItem.ReceivedBytes / 1024d / 1024d).ToString("0.00");
                DownloadProgress.TotalBytesToReceiveAsString = (downloadItem.TotalBytes / 1024d / 1024d).ToString("0.00");

                DownloadProgress.FileName = downloadItem.SuggestedFileName;

                if (downloadItem.IsInProgress && (downloadItem.PercentComplete != 0))
                    DownloadProgressChanged?.Invoke(this, DownloadProgress);
                
                if(downloadItem.IsComplete)
                {
                    SW.Stop();
                    _IsDownloadComplete = true;
                    OnFileDownloadCompleted?.Invoke(this, new FileDownloadRequestEvent(downloadItem.Url));
                }
            }
		}
	}
}