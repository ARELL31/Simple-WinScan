using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Exceptions;
using Simple_WinScan.Services.Implementations;
using Xunit;

namespace Simple_WinScan.Tests;

/// <summary>
/// Pruebas de regresión para WiaScannerService.
/// Valida que ante errores físicos WIA nunca se retorne una página falsa generada en memoria (bug previo),
/// sino que se propague ScannerHardwareException hacia la capa superior para alertar al usuario y permitir reintentos.
/// </summary>
public class WiaScannerServiceRegressionTests
{
    private class FakeWiaNativeInterop : IWiaNativeInterop
    {
        public Func<IReadOnlyList<ScannerDeviceInfo>>? OnEnumerateDevices { get; set; }
        public Func<ScannerDeviceInfo, ScanProfile, IProgress<int>?, CancellationToken, (byte[] RawBytes, int Width, int Height)>? OnTransferImage { get; set; }

        public IReadOnlyList<ScannerDeviceInfo> EnumerateDevices()
        {
            return OnEnumerateDevices != null ? OnEnumerateDevices() : new List<ScannerDeviceInfo>();
        }

        public (byte[] RawBytes, int Width, int Height) TransferImage(
            ScannerDeviceInfo scanner,
            ScanProfile profile,
            IProgress<int>? progress,
            CancellationToken cancellationToken)
        {
            if (OnTransferImage != null)
            {
                return OnTransferImage(scanner, profile, progress, cancellationToken);
            }
            return (new byte[] { 1, 2, 3, 4 }, 100, 100);
        }
    }

    [Fact]
    public async Task ScanPageAsync_WhenWiaHardwareFailsWithPaperJam_ThrowsScannerHardwareExceptionAndNeverReturnsMockPage()
    {
        var fakeInterop = new FakeWiaNativeInterop
        {
            OnTransferImage = (scanner, profile, prog, ct) =>
            {
                // WIA_ERROR_PAPER_JAM = 0x80210002
                throw new COMException("Paper jam in feeder", unchecked((int)0x80210002));
            }
        };

        var service = new WiaScannerService(fakeInterop);
        var testScanner = new ScannerDeviceInfo
        {
            Id = "TestScannerId",
            Name = "Escáner Físico de Prueba",
            Manufacturer = "WIA",
            IsDefault = true
        };
        var profile = ScanProfile.TextDefault;

        var exception = await Assert.ThrowsAsync<ScannerHardwareException>(async () =>
        {
            await service.ScanPageAsync(testScanner, profile, null, CancellationToken.None);
        });

        Assert.Equal(unchecked((int)0x80210002), exception.HResult);
        Assert.Contains("atasco de papel", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanPageAsync_WhenWiaHardwareIsOffline_ThrowsScannerHardwareExceptionWithOfflineDetails()
    {
        var fakeInterop = new FakeWiaNativeInterop
        {
            OnTransferImage = (scanner, profile, prog, ct) =>
            {
                // WIA_ERROR_OFFLINE = 0x80210005
                throw new COMException("Device offline or disconnected", unchecked((int)0x80210005));
            }
        };

        var service = new WiaScannerService(fakeInterop);
        var testScanner = new ScannerDeviceInfo
        {
            Id = "TestScannerId",
            Name = "Escáner Físico de Prueba",
            Manufacturer = "WIA",
            IsDefault = true
        };

        var exception = await Assert.ThrowsAsync<ScannerHardwareException>(async () =>
        {
            await service.ScanPageAsync(testScanner, ScanProfile.TextDefault, null, CancellationToken.None);
        });

        Assert.Equal(unchecked((int)0x80210005), exception.HResult);
        Assert.Contains("fuera de línea o desconectado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScanPageAsync_WhenTransferSucceeds_ReturnsPhysicalScannedPage()
    {
        var expectedBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var fakeInterop = new FakeWiaNativeInterop
        {
            OnTransferImage = (scanner, profile, prog, ct) => (expectedBytes, 1200, 1600)
        };

        var service = new WiaScannerService(fakeInterop);
        var testScanner = new ScannerDeviceInfo
        {
            Id = "PhysicalScanner1",
            Name = "Canon LiDE 300",
            Manufacturer = "Canon",
            IsDefault = true
        };

        var result = await service.ScanPageAsync(testScanner, ScanProfile.TextDefault, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedBytes, result.ImageBytes);
        Assert.Equal(1200, result.PixelWidth);
        Assert.Equal(1600, result.PixelHeight);
        Assert.Equal(300, result.Dpi);
    }

    [Fact]
    public async Task ScanPageAsync_WhenProgressReported_PropagatesToProgressCallback()
    {
        var reported = new List<int>();
        var progress = new Progress<int>(p => reported.Add(p));

        var fakeInterop = new FakeWiaNativeInterop
        {
            OnTransferImage = (scanner, profile, prog, ct) =>
            {
                prog?.Report(25);
                prog?.Report(50);
                prog?.Report(75);
                prog?.Report(100);
                return (new byte[] { 1, 2, 3 }, 800, 600);
            }
        };

        var service = new WiaScannerService(fakeInterop);
        var testScanner = new ScannerDeviceInfo { Id = "dev1", Name = "Epson Perfection", Manufacturer = "Epson" };

        var result = await service.ScanPageAsync(testScanner, ScanProfile.TextDefault, progress, CancellationToken.None);

        Assert.NotNull(result);
        // Dar tiempo a los despachos asíncronos de Progress<T>
        await Task.Delay(50);
        Assert.Contains(25, reported);
        Assert.Contains(50, reported);
        Assert.Contains(75, reported);
        Assert.Contains(100, reported);
    }

    [Fact]
    public async Task ScanPageAsync_WhenCancelled_ThrowsOperationCanceledExceptionPromptly()
    {
        var cts = new CancellationTokenSource();
        var fakeInterop = new FakeWiaNativeInterop
        {
            OnTransferImage = (scanner, profile, prog, ct) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return (new byte[] { 1, 2, 3 }, 800, 600);
            }
        };

        var service = new WiaScannerService(fakeInterop);
        var testScanner = new ScannerDeviceInfo { Id = "dev1", Name = "Epson Perfection", Manufacturer = "Epson" };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await service.ScanPageAsync(testScanner, ScanProfile.TextDefault, null, cts.Token);
        });
    }

    [Fact]
    public async Task ScanPageAsync_WhenDriverReturnsErrorCancelled_TranslatesToOperationCanceledException()
    {
        var fakeInterop = new FakeWiaNativeInterop
        {
            OnTransferImage = (scanner, profile, prog, ct) =>
            {
                // ERROR_CANCELLED = 0x800704C7
                throw new COMException("The user canceled the scan", unchecked((int)0x800704C7));
            }
        };

        var service = new WiaScannerService(fakeInterop);
        var testScanner = new ScannerDeviceInfo { Id = "dev1", Name = "Canon LiDE", Manufacturer = "Canon" };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await service.ScanPageAsync(testScanner, ScanProfile.TextDefault, null, CancellationToken.None);
        });
    }

    [Fact]
    public async Task GetAvailableScannersAsync_ReturnsPhysicalDevicesWithoutInjectingMock()
    {
        var fakeInterop = new FakeWiaNativeInterop
        {
            OnEnumerateDevices = () => new List<ScannerDeviceInfo>
            {
                new() { Id = "dev1", Name = "Epson Perfection V39", Manufacturer = "Epson", IsDefault = true }
            }
        };

        var service = new WiaScannerService(fakeInterop);
        var scanners = await service.GetAvailableScannersAsync();

        Assert.Single(scanners);
        Assert.Equal("Epson Perfection V39", scanners[0].Name);
        Assert.DoesNotContain(scanners, s => s.Id == MockScannerService.MockScannerId);
    }

    [Fact]
    public async Task GetAvailableScannersAsync_WhenNoDevicesPresent_ReturnsEmptyListWithoutMockFallback()
    {
        var fakeInterop = new FakeWiaNativeInterop
        {
            OnEnumerateDevices = () => new List<ScannerDeviceInfo>()
        };

        var service = new WiaScannerService(fakeInterop);
        var scanners = await service.GetAvailableScannersAsync();

        Assert.Empty(scanners);
    }
}
