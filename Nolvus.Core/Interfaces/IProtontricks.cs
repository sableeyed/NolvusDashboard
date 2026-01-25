namespace Nolvus.Core.Interfaces
{
    public interface IProtontricks : INolvusService
    {
        Task<int> RunAsync(string appId, params string[] args);
        Task<string?> GetPrefixPathAsync(string appId);
    }
}