namespace Simple_WinScan.Models;

public enum ScanMode
{
    Text,
    Photo
}

public enum ScanColorMode
{
    Color = 1,
    Grayscale = 2,
    BlackAndWhite = 4
}

public enum PageSizeFormat
{
    A4,
    Letter,
    Auto
}

public enum ScanSource
{
    Flatbed,
    Feeder,
    Duplex
}

public record ScanProfile
{
    public string Name { get; init; } = "Texto";
    public ScanMode Mode { get; init; } = ScanMode.Text;
    public int Dpi { get; init; } = 300;
    public ScanColorMode ColorMode { get; init; } = ScanColorMode.Grayscale;
    public PageSizeFormat PageFormat { get; init; } = PageSizeFormat.A4;
    public ScanSource Source { get; init; } = ScanSource.Flatbed;

    public static ScanProfile TextDefault => new()
    {
        Name = "Texto (300 DPI)",
        Mode = ScanMode.Text,
        Dpi = 300,
        ColorMode = ScanColorMode.Grayscale,
        PageFormat = PageSizeFormat.A4
    };

    public static ScanProfile PhotoDefault => new()
    {
        Name = "Foto (600 DPI)",
        Mode = ScanMode.Photo,
        Dpi = 600,
        ColorMode = ScanColorMode.Color,
        PageFormat = PageSizeFormat.A4
    };
}
