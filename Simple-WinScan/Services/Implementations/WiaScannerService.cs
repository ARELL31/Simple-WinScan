using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Exceptions;
using Simple_WinScan.Services.Interfaces;

namespace Simple_WinScan.Services.Implementations;

/// <summary>
/// Proveedor de escaneo físico nativo mediante Windows Image Acquisition (WIA) 2.0 en hilo STA dedicado.
/// </summary>
public class WiaScannerService : IScannerService
{
    private readonly IWiaNativeInterop _wiaInterop;

    public bool IsMockMode => false;

    public WiaScannerService() : this(new DefaultWiaNativeInterop())
    {
    }

    public WiaScannerService(IWiaNativeInterop wiaInterop)
    {
        _wiaInterop = wiaInterop ?? throw new ArgumentNullException(nameof(wiaInterop));
    }

    public Task<IReadOnlyList<ScannerDeviceInfo>> GetAvailableScannersAsync()
    {
        return RunInStaAsync(() =>
        {
            try
            {
                return _wiaInterop.EnumerateDevices();
            }
            catch (Exception ex)
            {
                // Si la consulta COM falla por permisos o servicio WIA detenido, reportar lista vacía sin simular
                System.Diagnostics.Debug.WriteLine($"[WiaScannerService] Error al enumerar escáneres: {ex.Message}");
                return (IReadOnlyList<ScannerDeviceInfo>)Array.Empty<ScannerDeviceInfo>();
            }
        });
    }

    public Task<ScannedPage> ScanPageAsync(ScannerDeviceInfo? scanner, ScanProfile profile, CancellationToken cancellationToken = default)
    {
        if (scanner == null)
        {
            throw new ArgumentNullException(nameof(scanner), "Se debe seleccionar un escáner válido para digitalizar.");
        }

        return RunInStaAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var (rawBytes, width, height) = _wiaInterop.TransferImage(scanner, profile, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (rawBytes == null || rawBytes.Length == 0)
                {
                    throw new ScannerHardwareException(
                        $"El escáner '{scanner.Name}' completó la transferencia sin transferir datos de imagen.",
                        unchecked((int)0x80210001),
                        scanner.Id,
                        scanner.Name);
                }

                return new ScannedPage
                {
                    ImageBytes = rawBytes,
                    PixelWidth = width,
                    PixelHeight = height,
                    Dpi = profile.Dpi,
                    RotationAngle = 0
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (COMException comEx)
            {
                // Mapear con precisión HRESULT a mensaje descriptivo en español
                throw ScannerHardwareException.FromComException(comEx, scanner.Id, scanner.Name);
            }
            catch (ScannerHardwareException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ScannerHardwareException(
                    $"Error inesperado durante la captura con el escáner '{scanner.Name}': {ex.Message}",
                    unchecked((int)0x80004005),
                    scanner.Id,
                    scanner.Name,
                    ex);
            }
        });
    }

    private static Task<T> RunInStaAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>();
        var thread = new Thread(() =>
        {
            try
            {
                var result = func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        return tcs.Task;
    }
}
