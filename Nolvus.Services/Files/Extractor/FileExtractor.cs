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

                    var sevenZip = "/usr/bin/7z";

                    var psi = new ProcessStartInfo
                    {
                        FileName = sevenZip,
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

                    TriggerProgressEvent(100, FileName);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log(ex.Message);
                    throw;
                }
            });
        }
    }
}
