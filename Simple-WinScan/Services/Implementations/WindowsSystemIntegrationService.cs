using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Simple_WinScan.Helpers;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Simple_WinScan.Services.Implementations;

public class WindowsSystemIntegrationService : ISystemIntegrationService
{
    private readonly IPdfExportService _pdfExportService;

    [ComImport]
    [Guid("3a3dcd6c-3eab-43dc-bcde-45671ce800c8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDataTransferManagerInterop
    {
        IntPtr GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);
        void ShowShareUIForWindow([In] IntPtr appWindow);
    }

    [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
    private static extern int RoGetActivationFactory(
        [MarshalAs(UnmanagedType.HString)] string activatableClassId,
        [In] ref Guid iid,
        out IDataTransferManagerInterop factory);

    public WindowsSystemIntegrationService(IPdfExportService pdfExportService)
    {
        _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
    }

    public Task<bool> OpenInExternalViewerAsync(ScannedPage page)
    {
        return Task.Run(() =>
        {
            try
            {
                byte[] processed = ImageHelper.ProcessImage(page.ImageBytes, page.RotationAngle, page.Crop);
                string tempFile = Path.Combine(Path.GetTempPath(), $"SimpleWinScan_Page_{Guid.NewGuid():N}.png");
                File.WriteAllBytes(tempFile, processed);

                var psi = new ProcessStartInfo(tempFile)
                {
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> OpenDocumentInExternalViewerAsync(IReadOnlyList<ScannedPage> pages)
    {
        if (pages == null || pages.Count == 0) return false;

        try
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"SimpleWinScan_Doc_{Guid.NewGuid():N}.pdf");
            using (var fs = File.Create(tempFile))
            {
                await _pdfExportService.ExportPagesToPdfAsync(pages, fs);
            }

            var psi = new ProcessStartInfo(tempFile)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> EmailPagesAsync(IReadOnlyList<ScannedPage> pages, string subject = "Documento Escaneado")
    {
        if (pages == null || pages.Count == 0) return false;

        try
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"SimpleWinScan_Doc_{Guid.NewGuid():N}.pdf");
            using (var fs = File.Create(tempFile))
            {
                await _pdfExportService.ExportPagesToPdfAsync(pages, fs);
            }

            // Iniciar cliente de correo predeterminado mediante mailto
            string mailtoUri = $"mailto:?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString($"Adjunto documento digitalizado con WinSimpleScan.\nArchivo temporal: {tempFile}")}";
            var psiMail = new ProcessStartInfo(mailtoUri)
            {
                UseShellExecute = true
            };
            try
            {
                Process.Start(psiMail);
            }
            catch { }

            // Resaltar el archivo PDF generado en el Explorador para facilitar adjuntar o arrastrar
            Process.Start("explorer.exe", $"/select,\"{tempFile}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SharePagesAsync(IReadOnlyList<ScannedPage> pages, string title = "Documento Escaneado")
    {
        if (pages == null || pages.Count == 0) return false;

        try
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"SimpleWinScan_Doc_{Guid.NewGuid():N}.pdf");
            using (var fs = File.Create(tempFile))
            {
                await _pdfExportService.ExportPagesToPdfAsync(pages, fs);
            }

            IntPtr hwnd = WindowHelper.GetWindowHandle();
            if (hwnd != IntPtr.Zero)
            {
                try
                {
                    Guid interopGuid = new Guid("3a3dcd6c-3eab-43dc-bcde-45671ce800c8");
                    int hr = RoGetActivationFactory("Windows.ApplicationModel.DataTransfer.DataTransferManager", ref interopGuid, out var factory);
                    if (hr == 0 && factory != null)
                    {
                        Guid dtmGuid = new Guid("a5caee9b-8708-49d1-8d36-67d25a8da00e");
                        IntPtr dtmPtr = factory.GetForWindow(hwnd, ref dtmGuid);
                        if (dtmPtr != IntPtr.Zero)
                        {
                            var dtm = WinRT.MarshalInterface<DataTransferManager>.FromAbi(dtmPtr);
                            dtm.DataRequested += async (s, e) =>
                            {
                                var deferral = e.Request.GetDeferral();
                                try
                                {
                                    e.Request.Data.Properties.Title = title;
                                    e.Request.Data.Properties.Description = "Documento PDF digitalizado";
                                    var storageFile = await StorageFile.GetFileFromPathAsync(tempFile);
                                    e.Request.Data.SetStorageItems(new[] { storageFile });
                                }
                                finally
                                {
                                    deferral.Complete();
                                }
                            };

                            factory.ShowShareUIForWindow(hwnd);
                            return true;
                        }
                    }
                }
                catch
                {
                    // Fallback si RoGetActivationFactory no está disponible en este hilo
                }
            }

            // Fallback: abrir en explorador para compartir
            Process.Start("explorer.exe", $"/select,\"{tempFile}\"");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
