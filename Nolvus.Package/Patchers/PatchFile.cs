using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Services;
using Nolvus.StockGame.Patcher;
using Nolvus.Package.Utilities;
using Nolvus.NexusApi.Responses;

namespace Nolvus.Package.Patchers
{
    public class PatchFile
    {
        public string OriginFileName { get; set; }
        public string DestinationFileName { get; set; }
        public string PatchFileName { get; set; }
        public string HashBefore { get; set; }
        public string HashAfter { get; set; }
        public string Directory { get; set; }

        public void Load(XmlNode Node)
        {
            OriginFileName = Node["OriginFileName"].InnerText;
            DestinationFileName = Node["DestinationFileName"].InnerText;
            PatchFileName = Node["PatchFileName"].InnerText;
            HashBefore = Node["HashBefore"].InnerText;
            HashAfter = Node["HashAfter"].InnerText;

            Directory = string.Empty;

            if (Node["Directory"] != null)
            {
                Directory = Node["Directory"].InnerText;
            }            
        }

        private FileInfo CopyPatchedFile(FileInfo Source, FileInfo Destination)
        {
            FileInfo Result;

            if (Source.Name == Destination.Name)
            {
                Destination.CopyTo(Source.FullName, true);
                Result = new FileInfo(Source.FullName);
            }
            else
            {
                Destination.CopyTo(Path.Combine(Source.DirectoryName, DestinationFileName), true);
                Result = new FileInfo(Path.Combine(Source.DirectoryName, DestinationFileName));
            }

            return Result;
        }
        public async Task Patch(string ModDir, string GameDir, string ExtractDir)
        {
            var Tsk = Task.Run(async () =>
            {
                try
                {
                    var PatcherManager = new PatcherManager(
                        ServiceSingleton.Folders.DownloadDirectory,
                        ServiceSingleton.Folders.LibDirectory,
                        ServiceSingleton.Folders.PatchDirectory);

                    // Determine working directory (mod or game)
                    var Dir = System.IO.Directory.Exists(ModDir) ? ModDir : GameDir;
                    Dir = PathResolver.ResolveCaseInsensitiveDirectory(null, Dir)
                        ?? Dir; // fallback if no changes

                    FileInfo? SourceFileToPatch = null;

                    if (string.IsNullOrWhiteSpace(Directory))
                    {
                        // -----------------------------------------------------------
                        // CASE 1: <Directory> is EMPTY → Search ENTIRE mod directory
                        // -----------------------------------------------------------
                        ServiceSingleton.Logger.Log($"[PATCH] Searching recursively in: {Dir}");

                        var allFiles = System.IO.Directory.GetFiles(Dir, "*", SearchOption.AllDirectories);

                        SourceFileToPatch = allFiles
                            .Where(f => Path.GetFileName(f)
                                .Equals(DestinationFileName, StringComparison.OrdinalIgnoreCase))
                            .Select(f => new FileInfo(f))
                            .Where(f => ServiceSingleton.Files.GetHash(f.FullName) == HashBefore)
                            .FirstOrDefault();
                    }
                    else
                    {
                        // -----------------------------------------------------------
                        // CASE 2: A Directory is provided → Try to resolve it
                        // -----------------------------------------------------------
                        string? resolvedFolder = PathResolver.ResolveCaseInsensitiveDirectory(Dir, Directory);

                        if (resolvedFolder != null)
                        {
                            ServiceSingleton.Logger.Log($"[PATCH] Searching in directory: {resolvedFolder}");

                            var files = System.IO.Directory.GetFiles(resolvedFolder, "*", SearchOption.AllDirectories);

                            SourceFileToPatch = files
                                .Where(f => Path.GetFileName(f)
                                    .Equals(DestinationFileName, StringComparison.OrdinalIgnoreCase))
                                .Select(f => new FileInfo(f))
                                .Where(f => ServiceSingleton.Files.GetHash(f.FullName) == HashBefore)
                                .FirstOrDefault();
                        }
                        else
                        {
                            ServiceSingleton.Logger.Log(
                                $"[PATCH] Could not resolve directory '{Directory}' inside '{Dir}'");
                        }
                    }

                    // -----------------------------------------------------------
                    // FAILURE: File not found anywhere
                    // -----------------------------------------------------------
                    if (SourceFileToPatch == null)
                    {
                        throw new Exception(
                            $"File name to patch does not exist ({DestinationFileName}) " +
                            $"hash : {HashBefore} in {Dir}");
                    }

                    // -----------------------------------------------------------
                    // SUCCESS: Found the file, now patch it
                    // -----------------------------------------------------------
                    ServiceSingleton.Logger.Log($"[PATCH] Patching file {SourceFileToPatch.FullName}");

                    var DestinationFileToPatch =
                        new FileInfo(Path.Combine(ExtractDir, DestinationFileName));

                    await PatcherManager.PatchFile(
                        SourceFileToPatch.FullName,
                        DestinationFileToPatch.FullName,
                        Path.Combine(ExtractDir, PatchFileName));

                    // Validate HashAfter
                    var newFile = CopyPatchedFile(SourceFileToPatch, DestinationFileToPatch);

                    if (ServiceSingleton.Files.GetHash(newFile.FullName) != HashAfter)
                    {
                        throw new Exception($"Hash for file '{DestinationFileName}' does not match after patch!");
                    }
                }
                catch (Exception ex)
                {
                    // Re-throw preserving stack trace
                    throw;
                }
            });

            await Tsk;
        }

    }
}
