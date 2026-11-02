using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using Simple_WinScan.ViewModels;

namespace Simple_WinScan.Services.Interfaces;

public class AutosavePageMetadata
{
    public int PageNumber { get; set; }
    public int RotationAngle { get; set; }
    public int Dpi { get; set; } = 300;
    public double CropX { get; set; }
    public double CropY { get; set; }
    public double CropWidth { get; set; } = 1.0;
    public double CropHeight { get; set; } = 1.0;
    public string ImageFileName { get; set; } = string.Empty;
}

public class AutosaveSession
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<AutosavePageMetadata> Pages { get; set; } = new();
}

public interface IAutosaveService
{
    Task<bool> HasAutosavedSessionAsync();
    Task<IReadOnlyList<ScannedPage>> RecoverSessionAsync();
    Task SaveSessionAsync(IReadOnlyList<ScannedPageItemViewModel> pages);
    Task ClearSessionAsync();
}
