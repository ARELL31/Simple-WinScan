using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;

namespace Simple_WinScan.Services.Interfaces;

public interface IPostprocessingService
{
    Task<ScannedPage> ProcessPageAsync(ScannedPage page, string commandTemplate, CancellationToken cancellationToken = default);
    Task<bool> RevertToOriginalAsync(ScannedPage page);
}
