using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Package.Services
{
    public class BSArchService : IBSArchService
    {
        private readonly string _bsarchDirectory;

        public BSArchService(string installDir)
        {
            _bsarchDirectory = Path.Combine(installDir, "TOOLS", "BSArch");

            if (!Directory.Exists(_bsarchDirectory))
                throw new DirectoryNotFoundException($"BSArch directory not found: {_bsarchDirectory}");
        }

        public async Task<bool> UnpackAsync(string bsaFile, string outputDirectory)
        {
            if (!File.Exists(bsaFile))
                throw new FileNotFoundException("BSA file not found", bsaFile);

            Directory.CreateDirectory(outputDirectory);

            var args = $"unpack \"{bsaFile}\" \"{outputDirectory}\"";

            var exitCode = await RunBSArchAsync(args);
            return exitCode == 0;
        }

        public async Task<bool> PackAsync(string sourceDirectory, string outputFile)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException("Source directory not found: " + sourceDirectory);

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

            var args = $"pack \"{sourceDirectory}\" \"{outputFile}\"";

            var exitCode = await RunBSArchAsync(args);
            return exitCode == 0;
        }

        public async Task<int> RunBSArchAsync(string arguments)
        {
            string exe = Path.Combine(_bsarchDirectory, "BSArch.exe");

            if (!File.Exists(exe))
                throw new FileNotFoundException("bsarch executable not found", exe);

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                WorkingDirectory = _bsarchDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = psi;

            var tcs = new TaskCompletionSource<int>();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.WriteLine($"BSArch: {e.Data}");
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    Console.WriteLine($"BSArch ERR: {e.Data}");
            };

            process.Exited += (_, __) =>
            {
                tcs.TrySetResult(process.ExitCode);
                process.Dispose();
            };

            process.EnableRaisingEvents = true;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return await tcs.Task;
        }
    }
}
