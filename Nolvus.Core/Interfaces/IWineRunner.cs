namespace Nolvus.Core.Interfaces
{
    public interface IWineRunner : INolvusService
    {
        Task<int> RunAsync(string workingDirectory, string exeName, params string[] args);
        string ToWinePath(string linuxPath);
    }

}