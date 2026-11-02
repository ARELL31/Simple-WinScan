namespace Simple_WinScan.Models;

public class AppSettings
{
    public string? LastSelectedScannerId { get; set; }
    public bool IsVirtualScannerEnabled { get; set; }
    public string? SelectedProfileName { get; set; } = "Texto (300 DPI)";
    public int ContinuousScanDelaySeconds { get; set; } = 5;
    public PageSizeFormat PaperSize { get; set; } = PageSizeFormat.A4;
    public int CustomDpi { get; set; } = 300;
    public int Brightness { get; set; } = 0;
    public int Contrast { get; set; } = 0;
    public string? PostprocessingCommand { get; set; }
    public bool IsPostprocessingEnabled { get; set; }
    public bool IsOcrEnabled { get; set; } = true;
    public string DefaultExportFormat { get; set; } = "PDF";
}
