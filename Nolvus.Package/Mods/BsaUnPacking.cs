using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nolvus.Core.Services;
using Nolvus.Core.Interfaces;
using Nolvus.Package.Services;
using System.Diagnostics;

namespace Nolvus.Package.Mods
{
    public class BsaUnPacking
    {
        public string FileName { get; set; }
        public string DirectoryName { get; set; }

        private FileInfo? GetBsaToUnpack(string extractDir)
        {
            var files = ServiceSingleton.Files.GetFiles(extractDir);

            if (string.IsNullOrWhiteSpace(DirectoryName))
            {
                return files.FirstOrDefault(x =>
                    x.Name.Equals(FileName, StringComparison.OrdinalIgnoreCase));
            }

            var normalizedDir = DirectoryName.Replace('\\', '/');

            return files.FirstOrDefault(f =>
            {
                if (!f.Name.Equals(FileName, StringComparison.OrdinalIgnoreCase))
                    return false;

                var dir = f.Directory.FullName.Replace('\\', '/');
                return dir.Contains(normalizedDir, StringComparison.OrdinalIgnoreCase);
            });
        }

        public async Task UnPack(string extractDir)
        {
            var Tsk = Task.Run(() =>
            {
                var bsaFile = GetBsaToUnpack(extractDir);
                var BSArch = Path.Combine(ServiceSingleton.Folders.LibDirectory, "BSArch");

                if (!File.Exists(BSArch))
                    throw new FileNotFoundException($"BSArch not found: {BSArch}", BSArch);

                if (bsaFile != null)
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = BSArch,
                        WorkingDirectory = ServiceSingleton.Folders.LibDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    psi.ArgumentList.Add("unpack");
                    psi.ArgumentList.Add(bsaFile.FullName);
                    psi.ArgumentList.Add(bsaFile.DirectoryName);

                    ServiceSingleton.Logger.Log($"Unpacking command line : \"{BSArch}\" unpack \"{bsaFile.FullName}\" \"{bsaFile.DirectoryName}\"");

                    var unpack = new Process { StartInfo = psi };
                    List<string> output = new();

                    unpack.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            output.Add(e.Data);
                        }
                    };

                    unpack.ErrorDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            output.Add(e.Data);
                        }
                    };

                    unpack.Start();
                    unpack.BeginOutputReadLine();
                    unpack.BeginErrorReadLine();
                    unpack.WaitForExit();

                    if (unpack.ExitCode == 0)
                    {
                        File.Delete(bsaFile.FullName);
                    }
                    else
                    {
                        throw new Exception("Failed to unpack file : " + FileName + "==>" + string.Join(Environment.NewLine, output.ToArray()));
                    }
                }
                else
                {
                    throw new Exception("Failed to unpack file : " + FileName + "==> File not found");
                }
            });

            await Tsk;
        }
    }
}
