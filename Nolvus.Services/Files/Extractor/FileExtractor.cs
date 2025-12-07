using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
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

        private void TriggerProgressEvent(int Percent, string FileName)
        {
            if (ExtractProgressChanged != null)
            {
                ExtractProgress.ProgressPercentage = Percent;
                ExtractProgress.FileName = FileName;
                ExtractProgressChanged(this, ExtractProgress);
            }
        }

        public async Task ExtractFile(string File, string Output, ExtractProgressChangedHandler OnProgress)
        {
            ServiceSingleton.Logger.Log("File to extract: " + File);
            ServiceSingleton.Logger.Log("Outpath path: " + Output);

            await Task.Run(() =>
            {
                FileName = Path.GetFileName(File);

                try
                {
                    if (OnProgress != null)
                        ExtractProgressChanged += OnProgress;

                    if (!Directory.Exists(Output))
                        Directory.CreateDirectory(Output);

                    var psi = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/7z",
                        Arguments = $"x -bsp1 -y \"{File}\" -o\"{Output}\" -mmt=off",
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
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                        throw new Exception($"Error during file extraction {FileName}: {string.Join(" ", errorOutput)}");

                    // -----------------------------------------
                    // Check for corrupted filenames (�)
                    // -----------------------------------------
                    if (ContainsCorruptedNames(Output))
                    {
                        ServiceSingleton.Logger.Log("[EXTRACT] Detected corrupted filenames (�) → switching to Ark fallback");

                        TryDeleteExtractDir(Output);
                        
                        var ark = new ProcessStartInfo
                        {
                            FileName = "/usr/bin/ark",
                            Arguments = $"-b \"{File}\" -o \"{Output}\"",
                            WorkingDirectory = ServiceSingleton.Folders.LibDirectory,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        var arkProc = new Process { StartInfo = ark };
                        List<string> arkErr = new();

                        arkProc.ErrorDataReceived += (s, e) =>
                        {
                            if (e.Data != null)
                                arkErr.Add(e.Data);
                        };

                        arkProc.Start();
                        arkProc.BeginErrorReadLine();
                        arkProc.WaitForExit();

                        if (arkProc.ExitCode != 0)
                            throw new Exception($"Ark extraction failed {FileName}: {string.Join(" ", arkErr)}");

                        ServiceSingleton.Logger.Log("[EXTRACT] Ark fallback completed successfully");
                    }
                    TriggerProgressEvent(100, FileName);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log(ex.Message);
                    throw;
                }
            });
        }

        private bool ContainsCorruptedNames(string root)
        {
            try
            {
                return Directory
                    .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .Any(f => Path.GetFileName(f).Contains('�'));
            }
            catch
            {
                return false;
            }
        }

        private void TryDeleteExtractDir(string path)
        {
            try
            {
                ServiceSingleton.Logger.Log("[EXTRACT] Removing corrupted extraction directory via rm -rf");

                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/bin/bash",
                    Arguments = $"-c \"rm -rf '{path}'/*\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var proc = new Process { StartInfo = psi };
                List<string> err = new();

                proc.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                        err.Add(e.Data);
                };

                proc.Start();
                proc.BeginErrorReadLine();
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                    ServiceSingleton.Logger.Log($"[EXTRACT] rm -rf exited with code {proc.ExitCode}: {string.Join(" ", err)}");
            }
            catch (Exception ex)
            {
                ServiceSingleton.Logger.Log($"[EXTRACT] Failed to delete directory {path} with rm -rf: {ex.Message}");
            }
        }
    }
}
