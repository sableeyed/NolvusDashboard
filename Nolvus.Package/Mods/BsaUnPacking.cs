using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nolvus.Core.Services;
using Nolvus.Core.Interfaces;
using Nolvus.Package.Services;

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
            var bsaFile = GetBsaToUnpack(extractDir);

            if (bsaFile == null)
                throw new Exception($"Failed to unpack file : {FileName} ==> File not found");

            ServiceSingleton.Logger.Log($"Unpacking BSA: {bsaFile.FullName}");

            // Get the BSArch service via ServiceSingleton
            var bsarch = ServiceSingleton.GetService<IBSArchService>();
            if (bsarch == null)
            {
                bsarch = new BSArchService(ServiceSingleton.Instances.WorkingInstance.InstallDir,
                                            ServiceSingleton.GetService<IWineRunner>());
                ServiceSingleton.RegisterService(bsarch);
            }

            bool success = await bsarch.UnpackAsync(
                bsaFile.FullName,
                bsaFile.DirectoryName
            );

            ServiceSingleton.Logger.Log($"BSArch result: success={success}");

            if (!success)
                throw new Exception($"Failed to unpack file: {FileName}");

            // Delete the BSA if extraction succeeded.
            File.Delete(bsaFile.FullName);
        }
    }
}
