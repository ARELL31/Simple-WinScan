using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;

namespace Simple_WinScan.Services.Interfaces;

public interface IPdfExportService
{
    Task ExportPagesToPdfAsync(IReadOnlyList<ScannedPage> pages, Stream outputStream, CancellationToken cancellationToken = default)
        => ExportPagesToPdfAsync(pages, outputStream, false, cancellationToken);

    Task ExportPagesToPdfAsync(IReadOnlyList<ScannedPage> pages, Stream outputStream, bool enableOcr, CancellationToken cancellationToken = default);
}
