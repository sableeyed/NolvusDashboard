using System.Diagnostics;
using System.Runtime.InteropServices;
using Nolvus.Core.Events;
using Nolvus.Core.Services;

namespace Nolvus.Services.Files.Extractor
{
    public class FileExtractor
    {
        private readonly ExtractProgress ExtractProgress;
        public event ExtractProgressChangedHandler ExtractProgressChanged;
        private string FileName;

        public FileExtractor()
        {
            ExtractProgress = new ExtractProgress();
        }

        private void TriggerProgressEvent(int percent, string fileName)
        {
            if (ExtractProgressChanged != null)
            {
                ExtractProgress.ProgressPercentage = percent;
                ExtractProgress.FileName = fileName;
                ExtractProgressChanged(this, ExtractProgress);
            }
        }

        public async Task ExtractFile(string file, string output, ExtractProgressChangedHandler onProgress)
        {
            ServiceSingleton.Logger.Log("File to extract: " + file);
            ServiceSingleton.Logger.Log("Outpath path: " + output);

            await Task.Run(() =>
            {
                FileName = Path.GetFileName(file);

                try
                {
                    if (onProgress != null)
                        ExtractProgressChanged += onProgress;

                    if (!Directory.Exists(output))
                        Directory.CreateDirectory(output);

                    var sevenZipPath = Path.Combine(ServiceSingleton.Folders.LibDirectory, "7z");

                    var psi = new ProcessStartInfo
                    {
                        FileName = sevenZipPath,
                        Arguments = $"x -bsp1 -y \"{file}\" -o\"{output}\" -mmt=off",
                        WorkingDirectory = ServiceSingleton.Folders.LibDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var proc = new Process { StartInfo = psi };
                    List<string> errorOutput = new();

                    proc.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null && e.Data.Length >= 4 && e.Data[3] == '%')
                        {
                            if (int.TryParse(e.Data.Substring(0, 3), out var pct))
                                TriggerProgressEvent(pct, FileName);
                        }
                    };

                    proc.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                            errorOutput.Add(e.Data);
                    };

                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    int exitCode = PosixWait.WaitForExitBlocking(proc.Id);
                    
                    try 
                    {
                        proc.Refresh();
                    } 
                    catch { }

                    if (exitCode != 0)
                        throw new Exception($"Error during file extraction {FileName} (exit code {exitCode}): {string.Join(" ", errorOutput)}");

                    TriggerProgressEvent(100, FileName);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log(ex.Message);
                    throw;
                }
                finally
                {
                    if (onProgress != null)
                    {
                        try 
                        {
                            ExtractProgressChanged -= onProgress;
                        } 
                        catch { }
                    }
                }
            });
        }

        private static class PosixWait
        {
            [DllImport("libc", SetLastError = true)]
            private static extern int waitpid(int pid, out int status, int options);

            // Wait until the given PID is reaped. No timeout.
            public static int WaitForExitBlocking(int pid)
            {
                while (true)
                {
                    int rc = waitpid(pid, out int status, 0);

                    if (rc == pid)
                        return DecodeExitCode(status);

                    if (rc == -1)
                    {
                        int err = Marshal.GetLastWin32Error();

                        // If errno == ECHILD, someone else already reaped it.
                        // Treat as success; extraction already finished.
                        const int ECHILD = 10;
                        if (err == ECHILD)
                            return 0;

                        throw new Exception($"waitpid({pid}) failed errno={err}");
                    }
                }
            }

            private static int DecodeExitCode(int status)
            {
                // Normal exit: low 7 bits are zero, exit code is high byte.
                if ((status & 0x7F) == 0)
                    return (status >> 8) & 0xFF;

                // Signaled: return 128+signal (bash convention)
                int sig = status & 0x7F;
                return 128 + sig;
            }
        }
    }
}
