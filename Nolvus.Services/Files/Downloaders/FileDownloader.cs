using System.Threading.Tasks;

namespace Nolvus.Services.Files.Downloaders
{
    public class FileDownloader : BaseFileDownloader
    {
        public override async Task DownloadFile(string UrlAddress, string Location)
        {
            await DownloadToFile(UrlAddress, Location);
        }
    }
}
