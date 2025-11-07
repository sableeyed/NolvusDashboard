using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Interfaces;
using System.Security.Cryptography;
using Nolvus.Core.Events;
using Nolvus.Core.Misc;
using Nolvus.Services.Files.Downloaders;
using Nolvus.Services.Files.Extractor;
using Force.Crc32;
using CG.Web.MegaApiClient;

namespace Nolvus.Services.Files
{    
    public class FileService : IFileService
    {        
        private MegaApiClient MegaApi = null;

        private MegaApiClient CreateMegaApi()
        {
            if (MegaApi == null)
            {
                MegaApi = new MegaApiClient();                
            }

            return MegaApi;
        }

        public async Task AuthenticateToMegaApi(string Username, string password)
        {
            var Tsk = Task.Run(async () => {
                
                try
                {
                    await CreateMegaApi().LoginAsync(Username, password);
                    await CreateMegaApi().LogoutAsync();
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
                
            });

            await Tsk;
        }

        public FileService()
        {            
        }

        private BaseFileDownloader CreateDownloader(string Url)
        {
            if (Url.Contains(Strings.GoogleDrive))
            {
                return new GoogleDriveFileDownloader();
            }
            else if (Url.Contains(Strings.Mega))
            {
                return new MegaFileDownloader(CreateMegaApi());
            }
            else
            {
                return new FileDownloader();
            }
        }       

        public async Task DownloadFile(string UrlAddress, string Location, DownloadProgressChangedHandler OnProgress)
        {
            using (var Downloader = CreateDownloader(UrlAddress))
            {                
                Downloader.DownloadProgressChanged += OnProgress;
                await Downloader.DownloadFile(UrlAddress, Location);               
            }
        }

        public async Task ExtractFile(string File, string Output, ExtractProgressChangedHandler OnProgress)
        {
            await new FileExtractor().ExtractFile(File, Output, OnProgress);
        }

        public void CopyFiles(string SourcePath, string TargetPath, bool IncludeRoot)
        {
            if (IncludeRoot)
            {
                Directory.CreateDirectory(TargetPath);
            }

            foreach (var DirPath in Directory.GetDirectories(SourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(DirPath.Replace(SourcePath, TargetPath));
            }

            foreach (var NewPath in Directory.GetFiles(SourcePath, "*.*", SearchOption.AllDirectories))
            {
                FileInfo FileSource = new FileInfo(NewPath);
                FileInfo FileDest = new FileInfo(NewPath.Replace(SourcePath, TargetPath));
                FileSource.CopyTo(FileDest.FullName, true);
            }
        }

        private void SetAttributesNormal(DirectoryInfo Directory)
        {
            foreach(var SubDir in Directory.GetDirectories())
            {
                SetAttributesNormal(SubDir);
            }

            foreach(var File in Directory.GetFiles())
            {
                File.Attributes = FileAttributes.Normal;
            }

            Directory.Attributes = FileAttributes.Normal;
        }

        public void RemoveDirectory(string DirectoryPath, bool RemoveDirectory)
        {
            DirectoryInfo _Directory = new DirectoryInfo(DirectoryPath);

            if (_Directory.Exists)
            {
                SetAttributesNormal(_Directory);

                foreach (FileInfo File in _Directory.GetFiles())
                {
                    File.Delete();
                }

                foreach (DirectoryInfo Directory in _Directory.GetDirectories())
                {
                    Directory.Delete(true);
                }

                if (RemoveDirectory)
                {
                    _Directory.Delete();
                }
            }            
        }

        public async Task<string> GetCRC32(FileInfo File, Action<string, int> Progress)
        {
            return await Task.Run(() =>
            {
                uint r = 0;
                using (var s = File.OpenRead())
                {
                    byte[] buff = new byte[1024];
                    int len = s.Read(buff, 0, buff.Length);
                    r = Crc32Algorithm.Compute(buff, 0, len);
                    uint Counter = 0;
                    int Internal = 0;
                    while ((len = s.Read(buff, 0, buff.Length)) > 0)
                    {
                        r = Crc32Algorithm.Append(r, buff, 0, len);

                        if (Progress != null)
                        {
                            Counter = Counter + 1024;

                            int Percent = System.Convert.ToInt16(Math.Round(((double)Counter / s.Length * 100)));

                            if (Percent > Internal)
                            {
                                Internal = Percent;
                                Progress(File.Name, Percent);
                            }
                        }                        
                    }
                }

                return Convert.ToString(r, 16).ToUpper();
            });

        }

        public FileInfo GetFileFromDirectory(string Directory, string FileName)
        {
            DirectoryInfo Di = new DirectoryInfo(Directory);

            return Di.GetFiles("*.*", SearchOption.AllDirectories).Where(x => x.Name == FileName).FirstOrDefault();
        }

        public List<FileInfo> GetFiles(string Directory)
        {            
            return new DirectoryInfo(Directory).GetFiles("*.*", SearchOption.AllDirectories).ToList();
        }

        public List<string> GetFilesPath(string Directory)
        {
            return GetFiles(Directory).Select(x=>x.FullName).ToList();
        }

        public bool FileExists(string Directory, string FileName)
        {
            return GetFileFromDirectory(Directory, FileName) != null;
        }

        public bool FileExists(string Directory, string FileName, out string FullPath)
        {
            FileInfo FileFromDirectory = GetFileFromDirectory(Directory, FileName);

            if (FileFromDirectory != null)
            {
                FullPath = FileFromDirectory.FullName;
                return true;
            }

            FullPath = string.Empty;

            return false;
        }

        public string GetHash(string FileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(FileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public bool IsDirectoryEmpty(string Dir)
        {
            return Directory.GetFiles(Dir, "*.*", SearchOption.AllDirectories).Length == 0;            
        }        
    }
}
