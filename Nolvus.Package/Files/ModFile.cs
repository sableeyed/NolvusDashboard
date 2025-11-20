using System;
using System.Diagnostics;
using System.Xml;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using Nolvus.Core.Events;
using Nolvus.Core.Services;
using Nolvus.Package.Mods;

namespace Nolvus.Package.Files
{
    public class ModFile
    {
        #region Properties

        public string Name { get; set; }
        public string FileName { get; set; }
        public bool TakenFromArchive
        {
            get
            {
                return Found(ServiceSingleton.Instances.WorkingInstance.ArchiveDir);
            }
        }
        public string LocationFileName
        {
            get
            {
                if (TakenFromArchive)
                {
                    string Path;

                    Found(ServiceSingleton.Instances.WorkingInstance.ArchiveDir, out Path);

                    return Path;
                }
                else
                {
                    return Path.Combine(ServiceSingleton.Folders.DownloadDirectory, FileName);
                }
            }
        }
        public int Size { get; set; }
        public string DownloadLink { get; set; }
        public string MirrorDownloadLink { get; set; }
        public string CRC32 { get; set; }
        public bool ExtractInSubdirectory { get; set; }
        public InstallableElement Element { get; set; }
        public bool RequireManualDownload { get; set; }
        public string VersionedFileName
        {
            get
            {
                return (Element.Name + "-v" + Element.Version + new FileInfo(FileName).Extension);
            }
        }              

        #endregion

        #region Methods

        public virtual void Load(XmlNode Node, InstallableElement InstallableElement)
        {
            Name = Node["Name"].InnerText;
            FileName = Node["FileName"].InnerText;
            Size = System.Convert.ToInt32(Node["Size"].InnerText);
            Element = InstallableElement;            
            CRC32 = Node["CRC32"].InnerText;
            DownloadLink = Node["DownloadLink"].InnerText;

            if (Node["RequireManualDownload"] != null)
            {
                RequireManualDownload = System.Convert.ToBoolean(Node["RequireManualDownload"].InnerText);
            }

            if (Node["MirrorDownloadLink"] != null)
            {
                MirrorDownloadLink = Node["MirrorDownloadLink"].InnerText;
            }
            else
            {
                MirrorDownloadLink = string.Empty;
            }
            
        }

        public override string ToString()
        {
            return FileName;
        }

        private bool Found(string Dir, out string FullPath)
        {
            FullPath = string.Empty;

            if (!(Dir != string.Empty))
            {
                return false;
            }

            if (!ServiceSingleton.Files.FileExists(Dir, FileName, out FullPath) && !ServiceSingleton.Files.FileExists(Dir, VersionedFileName, out FullPath))
            {
                return false;
            }

            return true;
        }

        private bool Found(string Dir)
        {
            string FullPath = string.Empty;

            if (!(Dir != string.Empty))
            {
                return false;
            }

            if (!ServiceSingleton.Files.FileExists(Dir, FileName, out FullPath) && !ServiceSingleton.Files.FileExists(Dir, VersionedFileName, out FullPath))
            {
                return false;
            }

            return true;
        }

        public bool Exist()
        {
            return Found(ServiceSingleton.Folders.DownloadDirectory) || Found(ServiceSingleton.Instances.WorkingInstance.ArchiveDir);
        }

        public FileInfo GetFileInfo()
        {
            return new FileInfo(LocationFileName);
        }

        public void Delete()
        {
            var FileInfo = GetFileInfo();

            if (FileInfo.Exists)
            {
                FileInfo.Delete();
            }
        }

        public async Task Move(string DestinationDirectory, Action<string, int> Progress)
        {
            var Tsk = Task.Run(() => 
            {
                var SourceFile = GetFileInfo();
                var ArchivedFile = new FileInfo(Path.Combine(DestinationDirectory, SourceFile.Name));

                if (!ArchivedFile.Exists)
                {
                    SourceFile.CopyTo(ArchivedFile, Progress);
                }

                SourceFile.Delete();
            });

            await Tsk;          
        }

        protected virtual async Task DoDownload(string Link, DownloadProgressChangedHandler OnProgress)
        {
            try
            {
                ServiceSingleton.Logger.Log(string.Format("Downloading file {0}", FileName));
                await ServiceSingleton.Files.DownloadFile(Link, Path.Combine(ServiceSingleton.Folders.DownloadDirectory, FileName), OnProgress);
            }
            catch (Exception ex)
            {                
                throw ex;
            }
        }

        public async Task<bool> CRCCheck(Action<string, int> HashProgress = null)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    var FileInfo = GetFileInfo();

                    ServiceSingleton.Logger.Log(string.Format("Checking CRC for file {0}", FileName));                    

                    if (CRC32 == string.Empty || !FileInfo.Exists || FileInfo.Length == 0 || (CRC32 != string.Empty && CRC32 != await ServiceSingleton.Files.GetCRC32(FileInfo, HashProgress)))
                    {
                        ServiceSingleton.Logger.Log(string.Format("CRC check failed for file {0}", FileName));
                        ServiceSingleton.Logger.Log(string.Format("Deleting bad file {0}", FileName));

                        Delete();

                        return false;
                    }

                    ServiceSingleton.Logger.Log(string.Format("CRC validated for file {0}", FileName));

                    return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
        }
        
        public async Task Download(DownloadProgressChangedHandler OnProgress, Action<string, int> HashProgress, int RetryCount, Func<IBrowserInstance> Browser)
        {
            //remove the task wrapping because of UI issues
            try
            {
                await InternalDownload(DownloadLink, OnProgress, HashProgress, RetryCount, Browser);
            }
            catch
            {
                if (MirrorDownloadLink != string.Empty)
                {
                    await InternalDownload(MirrorDownloadLink, OnProgress, HashProgress, RetryCount, Browser);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task InternalDownload(string Link, DownloadProgressChangedHandler OnProgress, Action<string, int> HashProgress, int RetryCount, Func<IBrowserInstance> Browser)
        {
            var Tries = 0;
            Exception CaughtException = null;

            if (!Exist())
            {
                try
                {
                    if (RequireManualDownload)
                    {
                        await Browser().AwaitUserDownload(Link, FileName, OnProgress);
                    }
                    else
                    {
                        await DoDownload(Link, OnProgress);
                    }
                }
                catch (Exception ex)
                {
                    CaughtException = ex.InnerException ?? ex;
                }
            }

            while (true)
            {
                if (await CRCCheck(HashProgress))
                    break;

                if (Tries == RetryCount)
                {
                    if (CaughtException != null)
                        throw new Exception($"Unable to download file {FileName}...", CaughtException);
                    else
                        throw new Exception($"Unable to download file {FileName} after retries!");
                }

                Tries++;

                await Task.Delay(200);
            }
        }
       

        public async Task Extract(ExtractProgressChangedHandler OnProgress)
        {
            var Tsk = Task.Run(async() => 
            {
                try
                {
                    ServiceSingleton.Logger.Log(string.Format("Extracting file {0}", FileName));
                    await ServiceSingleton.Files.ExtractFile(LocationFileName, Path.Combine(ServiceSingleton.Folders.ExtractDirectory, Element.ExtractSubDir), OnProgress);
                }
                catch(Exception ex)
                {
                    ServiceSingleton.Logger.Log(string.Format("Error during file extract {0} with error {1}", FileName, ex.Message));
                    throw ex;
                }
            });

            await Tsk;
        }

        public async Task Archive(string ArchivePath, Action<string, int> Progress)
        {
            var Tsk = Task.Run(async () => 
            {
                if (!Directory.Exists(ArchivePath))
                {
                    Directory.CreateDirectory(ArchivePath);
                }                

                if (!TakenFromArchive)
                {                
                    await Move(ArchivePath, Progress);                                   
                }                
            });

            await Tsk;
        }

        #endregion        
    }
}
