using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Nolvus.Core.Events;

namespace Nolvus.Services.Files.Downloaders
{
    public class GoogleDriveFileDownloader : BaseFileDownloader
    {
        private const int GOOGLE_DRIVE_MAX_ATTEMPT = 3;

        public event AsyncCompletedEventHandler? DownloadFileCompleted;

        private static readonly HttpClient _http = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            UseCookies = true
        });

        private static string ExtractGoogleDriveId(string url)
        {
            // Supports: id=, /file/d/ID/, resourcekey
            if (url.Contains("id="))
            {
                var idPart = url[(url.IndexOf("id=", StringComparison.Ordinal) + 3)..];
                var end = idPart.IndexOf('&');
                if (end > 0) idPart = idPart[..end];
                return idPart;
            }

            if (url.Contains("/file/d/"))
            {
                var start = url.IndexOf("/file/d/") + 8;
                var end = url.IndexOf('/', start);
                if (end < 0) end = url.IndexOf('?', start);
                if (end < 0) end = url.Length;
                return url[start..end];
            }

            return string.Empty;
        }

        private static string BuildDownloadUrl(string fileId, string? resourceKey)
        {
            if (!string.IsNullOrWhiteSpace(resourceKey))
                return $"https://drive.google.com/uc?id={fileId}&export=download&resourcekey={resourceKey}&confirm=t";

            return $"https://drive.google.com/uc?id={fileId}&export=download&confirm=t";
        }

        private static string? ExtractResourceKey(string url)
        {
            var idx = url.IndexOf("resourcekey=", StringComparison.Ordinal);
            if (idx < 0) return null;

            var start = idx + "resourcekey=".Length;
            var end = url.IndexOf('&', start);

            if (end < 0) return url[start..];
            return url[start..end];
        }

        private async Task<bool> DetectGoogleConfirmationAndRetry(string filePath, string originalUrl, int attempt)
        {
            var fi = new FileInfo(filePath);
            if (!fi.Exists || fi.Length > 60000) // file is too large to be an HTML prompt
                return false;

            string text = await File.ReadAllTextAsync(filePath);
            if (!text.Contains("<!DOCTYPE html>"))
                return false;

            // Search for confirm download link
            var linkIdx = text.LastIndexOf("href=\"/uc?", StringComparison.Ordinal);
            if (linkIdx < 0)
                return false;

            linkIdx += 6;
            var end = text.IndexOf('"', linkIdx);
            if (end < 0) return false;

            var confirmUrl = "https://drive.google.com" +
                             text.Substring(linkIdx, end - linkIdx).Replace("&amp;", "&");

            if (attempt >= GOOGLE_DRIVE_MAX_ATTEMPT)
                return false;

            // Retry: overwrite file and download the confirmed url
            await DownloadToFile(confirmUrl, filePath);
            return true;
        }

        public override async Task DownloadFile(string UrlAddress, string Location)
        {
            Progress.FileName = Path.GetFileName(Location);

            var fileId = ExtractGoogleDriveId(UrlAddress);
            if (string.IsNullOrEmpty(fileId))
            {
                // Not actually Google Drive → standard download
                await DownloadToFile(UrlAddress, Location);
                return;
            }

            var resourceKey = ExtractResourceKey(UrlAddress);
            var downloadUrl = BuildDownloadUrl(fileId, resourceKey);

            int attempt = 1;

            while (true)
            {
                await DownloadToFile(downloadUrl, Location);

                if (!await DetectGoogleConfirmationAndRetry(Location, UrlAddress, attempt))
                    break;

                attempt++;
            }

            DownloadFileCompleted?.Invoke(this, new AsyncCompletedEventArgs(null, false, null));
        }
    }
}
