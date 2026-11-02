using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;

namespace Simple_WinScan.Services.Interfaces;

public class PrintOptions
{
    public string? PrinterName { get; set; }
    public bool Landscape { get; set; }
    public int Copies { get; set; } = 1;
}

public interface IPrintService
{
    IReadOnlyList<string> GetInstalledPrinters();
    string? GetDefaultPrinter();
    Task<bool> PrintPagesAsync(IReadOnlyList<ScannedPage> pages, PrintOptions? options = null, CancellationToken cancellationToken = default);
}
