using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Nolvus.Core.Services;
using Nolvus.Package.Services;
using System.Threading.Tasks;

namespace Nolvus.Package.Mods
{
    public class BsaUnPacking
    {
        public string FileName { get; set; }
        public string DirectoryName { get; set; }

        private FileInfo GetBsaToUnpack(string ExtractDir)
        {            
            if (DirectoryName == string.Empty)
            {
                return ServiceSingleton.Files.GetFiles(ExtractDir).Where(x => x.Name == FileName).FirstOrDefault();
            }
            else
            {
                return ServiceSingleton.Files.GetFiles(ExtractDir).Where(x => x.Name == FileName && x.Directory.FullName.Contains(DirectoryName)).FirstOrDefault();
            }            
        }

        public async Task UnPack(string extractDir)
        {
            var bsaFile = GetBsaToUnpack(extractDir);

            if (bsaFile == null)
                throw new Exception($"Failed to unpack file : {FileName} ==> File not found");

            // Create service (or inject via constructor)
            var bsarch = new BSArchService(ServiceSingleton.Instances.WorkingInstance.InstallDir);

            ServiceSingleton.Logger.Log($"Unpacking BSA: {bsaFile.FullName}");

            bool success = await bsarch.UnpackAsync(bsaFile.FullName, bsaFile.DirectoryName);

            if (!success)
                throw new Exception($"Failed to unpack file: {FileName}");

            // Delete after successful extraction
            File.Delete(bsaFile.FullName);
        }

    }
}
