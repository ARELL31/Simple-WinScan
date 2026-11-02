using System;
using System.Collections.Generic;

namespace Simple_WinScan.Models;

public record ScannerCapabilities
{
    public IReadOnlyList<int> SupportedDpis { get; init; } = new[] { 75, 150, 200, 300, 600, 1200 };
    public bool SupportsBrightness { get; init; } = true;
    public bool SupportsContrast { get; init; } = true;
    public bool SupportsFeeder { get; init; } = true;
    public bool SupportsDuplex { get; init; } = false;
    public IReadOnlyList<PageSizeFormat> SupportedPaperSizes { get; init; } = new[]
    {
        PageSizeFormat.A4,
        PageSizeFormat.Letter,
        PageSizeFormat.Auto
    };

    public static ScannerCapabilities Default => new();
}
