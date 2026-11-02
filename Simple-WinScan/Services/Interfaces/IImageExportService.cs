using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;

namespace Simple_WinScan.Services.Interfaces;

public enum ImageExportFormat
{
    Jpeg,
    Png,
    WebP
}

public interface IImageExportService
{
    Task ExportPageAsync(ScannedPage page, Stream outputStream, ImageExportFormat format, int quality = 90, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ExportPagesAsync(IReadOnlyList<ScannedPage> pages, string baseFilePath, ImageExportFormat format, int quality = 90, CancellationToken cancellationToken = default);
}
