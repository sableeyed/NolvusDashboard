using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Nolvus.Core.Events;
using Nolvus.Core.Services;

namespace Nolvus.Services.Files
{
    public abstract class BaseFileDownloader : IDisposable
    {
        protected Stopwatch SW = new Stopwatch();
        private readonly HttpClient _http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true
        });

        event DownloadProgressChangedHandler DownloadProgressChangedEvent;

        public event DownloadProgressChangedHandler DownloadProgressChanged
        {
            add
            {
                lock (this)
                    DownloadProgressChangedEvent += value;
            }
            remove
            {
                lock (this)
                    DownloadProgressChangedEvent -= value;
            }
        }

        protected readonly DownloadProgress Progress = new DownloadProgress();
        protected string FileName = string.Empty;

        protected void NotifyProgress()
        {
            DownloadProgressChangedEvent?.Invoke(this, Progress);
        }

        public abstract Task DownloadFile(string UrlAddress, string Location);

        protected async Task DownloadToFile(string url, string path)
        {
            FileName = Path.GetFileName(path);

            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            Progress.TotalBytesToReceive = response.Content.Headers.ContentLength ?? -1;
            Progress.BytesReceived = 0;
            SW.Restart();

            await using var input = await response.Content.ReadAsStreamAsync();
            await using var output = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[64 * 1024];
            int read;
            while ((read = await input.ReadAsync(buffer)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read));
                Progress.BytesReceived += read;

                Progress.ProgressPercentage = Progress.TotalBytesToReceive > 0
                    ? (int)((double)Progress.BytesReceived / Progress.TotalBytesToReceive * 100)
                    : 0;

                Progress.Speed = Progress.BytesReceived / 1024d / 1024d / SW.Elapsed.TotalSeconds;
                Progress.FileName = FileName;

                NotifyProgress();
            }

            SW.Stop();
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
