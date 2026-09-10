using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Nolvus.Core.Services;

namespace Nolvus.Core.Utils
{
    /// <summary>
    /// Keeps the .NET runtime's SIGCHLD handler installed for the lifetime of the process.
    ///
    /// The runtime reaps child processes from a signal handler it installs lazily, the first time
    /// anything calls Process.Start. Every managed wait is built on that: Process.WaitForExit(),
    /// WaitForExitAsync() and even Process.HasExited all block until the handler observes the child
    /// exiting - none of them call waitpid on their own.
    ///
    /// CEF clears that handler back to SIG_DFL for the whole process. From that moment on, every
    /// helper process we start (7z, xdelta3, BSArch, wget) runs to completion, becomes an unreaped
    /// zombie, and the wait on it blocks forever - so the install pipeline stalls with no error and
    /// no log line, most visibly right after extraction. The signature is a "&lt;defunct&gt;" child in
    /// ps and a cleared bit 16 in SigCgt in /proc/&lt;pid&gt;/status.
    ///
    /// This class snapshots the runtime's handler while it is still intact and reinstalls it
    /// whenever it has been cleared. Reinstalling alone only rescues children that exit afterwards,
    /// so it also raises SIGCHLD against our own process, which makes the runtime's reaper rescan
    /// every child it is tracking and pick up any that already exited while the handler was gone.
    /// That releases waits which are already stuck, so a clobber that lands mid-extraction is
    /// recovered rather than fatal.
    /// </summary>
    public static class SigChldGuard
    {
        private const int SIGCHLD = 17;
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

        private static SigAction _runtimeHandler;
        private static bool _captured;
        private static Timer? _watchdog;
        private static int _restoreCount;

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct SigAction
        {
            public IntPtr sa_handler;
            public fixed ulong sa_mask[16];
            public int sa_flags;
            public IntPtr sa_restorer;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern unsafe int sigaction(int signum, SigAction* act, SigAction* oldact);

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int sig);

        /// <summary>
        /// Call once during startup, before CEF is initialized.
        /// </summary>
        public static void Install()
        {
            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
                return;

            try
            {
                Capture();

                if (!_captured)
                {
                    ServiceSingleton.Logger.Log(
                        "[SIGCHLD] Could not capture the runtime child handler; process waits are unprotected.");
                    return;
                }

                _watchdog = new Timer(_ => EnsureInstalled(), null, PollInterval, PollInterval);

                ServiceSingleton.Logger.Log("[SIGCHLD] Child reaping handler captured and guarded.");
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[SIGCHLD] Guard could not be installed: {ex.Message}");
            }
        }

        /// <summary>
        /// Reinstalls the runtime's handler if something has cleared it. Safe to call at any time.
        /// </summary>
        public static unsafe void EnsureInstalled()
        {
            if (!_captured)
                return;

            try
            {
                SigAction current;

                if (sigaction(SIGCHLD, null, &current) != 0)
                    return;

                if (current.sa_handler == _runtimeHandler.sa_handler)
                    return;

                fixed (SigAction* restore = &_runtimeHandler)
                {
                    if (sigaction(SIGCHLD, restore, null) != 0)
                        return;
                }

                // Reinstalling only covers children that exit from here on. Raising SIGCHLD against
                // ourselves makes the runtime rescan its children and reap any that already exited
                // while the handler was missing, releasing waits that are stuck right now.
                kill(Environment.ProcessId, SIGCHLD);

                _restoreCount++;

                ServiceSingleton.Logger.Log(
                    $"[SIGCHLD] Handler had been cleared; reinstalled and rescanned children (restore #{_restoreCount}).");
            }
            catch
            {
                // Never let the watchdog take the process down.
            }
        }

        private static unsafe void Capture()
        {
            // The runtime installs the handler lazily on the first Process.Start, so make sure one
            // has happened before snapshotting. Waiting on it is safe here: nothing has clobbered
            // SIGCHLD yet this early in startup, and the wait is bounded regardless.
            if (!HandlerPresent())
            {
                try
                {
                    using var warmup = Process.Start(new ProcessStartInfo
                    {
                        FileName = "/bin/sh",
                        Arguments = "-c \"exit 0\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    });

                    warmup?.WaitForExit(5000);
                }
                catch
                {
                    // Fall through; the check below decides whether we got a handler.
                }
            }

            if (!HandlerPresent())
                return;

            fixed (SigAction* saved = &_runtimeHandler)
            {
                if (sigaction(SIGCHLD, null, saved) == 0)
                    _captured = true;
            }
        }

        private static unsafe bool HandlerPresent()
        {
            SigAction current;

            if (sigaction(SIGCHLD, null, &current) != 0)
                return false;

            // SIG_DFL is 0 and SIG_IGN is 1; anything else is a real handler function.
            return current.sa_handler != IntPtr.Zero && current.sa_handler != new IntPtr(1);
        }
    }
}
