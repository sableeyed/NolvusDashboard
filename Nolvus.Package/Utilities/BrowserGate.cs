using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Nolvus.Core.Services;

namespace Nolvus.Package.Utilities
{
    /// <summary>
    /// Serializes every interactive browser window opened during an install.
    ///
    /// ModInstallSettings.Browser creates and shows a new BrowserWindow on each call, so anything
    /// that asks for one while another is open puts a second window on screen and asks the user to
    /// deal with both at once. There are two such call sites - resolving a manual Nexus download
    /// link (InstallableElement.RequestManualNexusDownloadLink) and driving a download to completion
    /// in the browser (ModFile.InternalDownload, for RequireManualDownload files such as the ENB
    /// binaries) - and they must share one gate to be exclusive of each other.
    ///
    /// Scope this as tightly as possible: hold it only around the browser interaction itself, never
    /// around hashing, downloading or extraction. Widening it serializes the whole install, which is
    /// what made installing from an existing archive so slow for free accounts.
    /// </summary>
    public static class BrowserGate
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);
        private static int _waiting;

        public static async Task RunAsync(string Context, Func<Task> Action)
        {
            await AcquireAsync(Context).ConfigureAwait(false);

            try
            {
                await Action().ConfigureAwait(false);
            }
            finally
            {
                Gate.Release();
            }
        }

        public static async Task<T> RunAsync<T>(string Context, Func<Task<T>> Action)
        {
            await AcquireAsync(Context).ConfigureAwait(false);

            try
            {
                return await Action().ConfigureAwait(false);
            }
            finally
            {
                Gate.Release();
            }
        }

        private static async Task AcquireAsync(string Context)
        {
            if (Gate.Wait(0))
                return;

            // Contended: another mod already has the browser. Log it so a queue of mods waiting on
            // the user is visible in the log rather than looking like a stall.
            var Queued = Interlocked.Increment(ref _waiting);
            var Watch = Stopwatch.StartNew();

            ServiceSingleton.Logger.Log(
                $"[BROWSER] Waiting for the browser ({Queued} queued) : {Context}");

            try
            {
                await Gate.WaitAsync().ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _waiting);
            }

            ServiceSingleton.Logger.Log(
                $"[BROWSER] Browser acquired after {Watch.Elapsed.TotalSeconds:0.0}s : {Context}");
        }
    }
}
