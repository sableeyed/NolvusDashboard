using System;
using System.Threading.Tasks;

namespace Nolvus.Core.Interfaces
{
    public interface IBrowserInstance : IAsyncDisposable
    {
        Task RunAsync(string url, CancellationToken cancellationToken = default);
    }
}
