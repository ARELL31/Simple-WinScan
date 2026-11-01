namespace Simple_WinScan.Models;

public record ScannerDeviceInfo
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public bool IsDefault { get; init; }

    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? "Escáner WIA" : Name;
}
