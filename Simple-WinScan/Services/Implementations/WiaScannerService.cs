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
/// Proveedor de escaneo WIA 2.0 nativo ejecutado en un hilo STA dedicado.
/// Implementa cancelación no bloqueante y reporte cuantitativo de progreso hacia la UI.
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
                System.Diagnostics.Debug.WriteLine($"[WiaScannerService] Error al enumerar escáneres: {ex.Message}");
                return (IReadOnlyList<ScannerDeviceInfo>)Array.Empty<ScannerDeviceInfo>();
            }
        });
    }

    public Task<ScannedPage> ScanPageAsync(
        ScannerDeviceInfo? scanner,
        ScanProfile profile,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
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
                var (rawBytes, width, height) = _wiaInterop.TransferImage(scanner, profile, progress, cancellationToken);

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
                // Detectar si fue cancelación solicitada por el usuario o driver abortado
                if (cancellationToken.IsCancellationRequested ||
                    comEx.ErrorCode == unchecked((int)0x800704C7) || // ERROR_CANCELLED (0x4C7)
                    comEx.ErrorCode == unchecked((int)0x80004004) || // E_ABORT
                    comEx.ErrorCode == unchecked((int)0x80210064))   // WIA_STATUS_WARMING_UP / Cancel error
                {
                    throw new OperationCanceledException("La captura fue cancelada por el usuario.", comEx, cancellationToken);
                }

                // Mapear con precisión HRESULT a mensaje descriptivo en español
                throw ScannerHardwareException.FromComException(comEx, scanner.Id, scanner.Name);
            }
            catch (ScannerHardwareException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("La captura fue cancelada por el usuario.", ex, cancellationToken);
                }

                throw new ScannerHardwareException(
                    $"Error inesperado durante la captura con el escáner '{scanner.Name}': {ex.Message}",
                    unchecked((int)0x80004005),
                    scanner.Id,
                    scanner.Name,
                    ex);
            }
        }, cancellationToken);
    }

    private static Task<T> RunInStaAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled(cancellationToken);
            });
        }

        var thread = new Thread(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                var result = func();

                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                else
                {
                    tcs.TrySetResult(result);
                }
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                else
                {
                    tcs.TrySetException(ex);
                }
            }
            finally
            {
                registration.Dispose();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        return tcs.Task;
    }
}
