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

            await Task.Run(async () =>
            {
                FileName = Path.GetFileName(File);

                try
                {
                    if (OnProgress != null)
                        ExtractProgressChanged += OnProgress;

                    if (!Directory.Exists(Output))
                        Directory.CreateDirectory(Output);

                    var sevenZipPath = Path.Combine(ServiceSingleton.Folders.LibDirectory, "7z");

                    var psi = new ProcessStartInfo
                    {
                        FileName = sevenZipPath,
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

                    await proc.WaitForExitAsync();

                    int exitCode = proc.ExitCode;

                    if (exitCode != 0)
                        throw new Exception($"Error during File extraction {FileName} (exit code {exitCode}): {string.Join(" ", errorOutput)}");

                    SplitBackslashPaths(Output);

                    TriggerProgressEvent(100, FileName);
                }
                catch (Exception ex)
                {
                    ServiceSingleton.Logger.Log(ex.Message);
                    throw;
                }
                finally
                {
                    if (OnProgress != null)
                    {
                        try 
                        {
                            ExtractProgressChanged -= OnProgress;
                        } 
                        catch { }
                    }
                }
            });
        }

        /// <summary>
        /// Zips made on Windows can store paths with backslashes (Seasons\foo.ini). 7z on Linux takes
        /// those as part of a single file name rather than as separators, so the archive comes out
        /// flat, with every rule pointing into a subfolder missing its file. Rebuilds the folders
        /// Windows would have extracted.
        /// </summary>
        private static void SplitBackslashPaths(string Root)
        {
            var Files = Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
                .Where(x => Path.GetRelativePath(Root, x).Contains('\\'))
                .ToList();

            if (Files.Count == 0)
                return;

            foreach (var File_ in Files)
            {
                var Target = Path.Combine(Root, Path.GetRelativePath(Root, File_).Replace('\\', '/'));

                Directory.CreateDirectory(Path.GetDirectoryName(Target)!);
                File.Move(File_, Target, overwrite: true);
            }

            // Directory entries (Seasons\) come out as folders with a backslash in their name, empty
            // once their files have moved. Deepest first so nested ones are empty when reached.
            foreach (var Dir in Directory.EnumerateDirectories(Root, "*", SearchOption.AllDirectories)
                .Where(x => Path.GetFileName(x).Contains('\\'))
                .OrderByDescending(x => x.Length)
                .ToList())
            {
                if (!Directory.EnumerateFileSystemEntries(Dir).Any())
                    Directory.Delete(Dir);
            }

            ServiceSingleton.Logger.Log($"Split {Files.Count} backslash path(s) into folders in {Root}");
        }
    }
}
