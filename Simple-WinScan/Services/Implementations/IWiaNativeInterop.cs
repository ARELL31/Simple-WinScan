using System;
using System.Collections.Generic;
using System.Threading;
using Simple_WinScan.Models;

namespace Simple_WinScan.Services.Implementations;

/// <summary>
/// Abstracción interna para desacoplar las llamadas COM nativas WIA 2.0 y permitir pruebas automatizadas de regresión.
/// </summary>
public interface IWiaNativeInterop
{
    IReadOnlyList<ScannerDeviceInfo> EnumerateDevices();
    (byte[] RawBytes, int Width, int Height) TransferImage(ScannerDeviceInfo scanner, ScanProfile profile, IProgress<int>? progress, CancellationToken cancellationToken);
}
