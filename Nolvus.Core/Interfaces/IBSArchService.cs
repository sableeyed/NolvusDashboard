using System.Threading.Tasks;

namespace Nolvus.Core.Interfaces
{
    public interface IBSArchService : INolvusService
    {
        Task<bool> UnpackAsync(string bsaFile, string outputDirectory);
    }
}
