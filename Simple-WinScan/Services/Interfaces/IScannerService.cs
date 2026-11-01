using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;

namespace Simple_WinScan.Services.Interfaces;

public interface IScannerService
{
    Task<IReadOnlyList<ScannerDeviceInfo>> GetAvailableScannersAsync();
    Task<ScannedPage> ScanPageAsync(ScannerDeviceInfo? scanner, ScanProfile profile, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    bool IsMockMode { get; }
}
