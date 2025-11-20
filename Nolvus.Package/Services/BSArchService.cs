using System;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Services;

namespace Nolvus.Package.Services
{
    public class BSArchService : IBSArchService
    {
        private readonly string _bsarchDirectory;
        private readonly IWineRunner _wineRunner;

        private const string BsarchExe = "BSArch.exe";

        public BSArchService(string installDir, IWineRunner wineRunner)
        {
            _bsarchDirectory = Path.Combine(installDir, "TOOLS", "BSArch");
            _wineRunner = wineRunner ?? throw new ArgumentNullException(nameof(wineRunner));

            if (!Directory.Exists(_bsarchDirectory))
                throw new DirectoryNotFoundException($"BSArch directory not found: {_bsarchDirectory}");
        }

        public async Task<bool> UnpackAsync(string bsaFile, string outputDirectory)
        {
            if (!File.Exists(bsaFile))
                throw new FileNotFoundException("BSA file not found", bsaFile);

            Directory.CreateDirectory(outputDirectory);

            string winBsa = _wineRunner.ToWinePath(bsaFile);
            string winOut = _wineRunner.ToWinePath(outputDirectory);
            string workingDir = _bsarchDirectory;

            ServiceSingleton.Logger.Log($"[BSARCH] Running Wine: unpack {winBsa} {winOut}");

            int exit = await _wineRunner.RunAsync(
                workingDir,
                BsarchExe,
                "unpack",
                winBsa,
                winOut
            );

            ServiceSingleton.Logger.Log($"[BSARCH] Exit code: {exit}");

            return exit == 0;
        }
    }
}
