using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Nolvus.Core.Events;
using Nolvus.Core.Services;

namespace Nolvus.Services.Files.Downloaders
{
    public class FileDownloader : BaseFileDownloader
    {        
        public override async Task DownloadFile(string UrlAddress, string Location)
        {                       
            FileName = new FileInfo(Location).Name;                

            SW.Start();

            await Client.DownloadFileTaskAsync(new Uri(UrlAddress), Location);                

            SW.Stop();            
        }
    }
}
