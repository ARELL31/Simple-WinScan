using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Helpers;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;

namespace Simple_WinScan.Services.Implementations;

public class SystemDrawingPrintService : IPrintService
{
    public IReadOnlyList<string> GetInstalledPrinters()
    {
        try
        {
            return PrinterSettings.InstalledPrinters.Cast<string>().ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public string? GetDefaultPrinter()
    {
        try
        {
            var settings = new PrinterSettings();
            return settings.PrinterName;
        }
        catch
        {
            return null;
        }
    }

    public Task<bool> PrintPagesAsync(IReadOnlyList<ScannedPage> pages, PrintOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (pages == null || pages.Count == 0)
        {
            return Task.FromResult(false);
        }

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var printDoc = new PrintDocument();
            printDoc.DocumentName = "WinSimpleScan_Documento";

            if (!string.IsNullOrWhiteSpace(options?.PrinterName))
            {
                printDoc.PrinterSettings.PrinterName = options.PrinterName;
            }

            if (options != null)
            {
                printDoc.PrinterSettings.Copies = (short)Math.Max(1, options.Copies);
                printDoc.DefaultPageSettings.Landscape = options.Landscape;
            }

            // Deshabilitar diálogo visual de progreso nativo para evitar cuelgues de ventana
            printDoc.PrintController = new StandardPrintController();

            int currentPageIndex = 0;

            printDoc.PrintPage += (sender, e) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    e.Cancel = true;
                    return;
                }

                if (currentPageIndex >= pages.Count)
                {
                    e.HasMorePages = false;
                    return;
                }

                var page = pages[currentPageIndex];
                byte[] processedBytes = ImageHelper.ProcessImage(page.ImageBytes, page.RotationAngle, page.Crop);

                using (var ms = new MemoryStream(processedBytes))
                using (var img = System.Drawing.Image.FromStream(ms))
                {
                    var printableArea = e.MarginBounds;
                    if (printableArea.Width <= 0 || printableArea.Height <= 0)
                    {
                        printableArea = e.PageBounds;
                    }

                    float scaleW = (float)printableArea.Width / img.Width;
                    float scaleH = (float)printableArea.Height / img.Height;
                    float scale = Math.Min(scaleW, scaleH);

                    float drawW = img.Width * scale;
                    float drawH = img.Height * scale;
                    float drawX = printableArea.Left + (printableArea.Width - drawW) / 2.0f;
                    float drawY = printableArea.Top + (printableArea.Height - drawH) / 2.0f;

                    if (e.Graphics != null)
                    {
                        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        e.Graphics.DrawImage(img, drawX, drawY, drawW, drawH);
                    }
                }

                currentPageIndex++;
                e.HasMorePages = currentPageIndex < pages.Count;
            };

            printDoc.Print();
            return true;
        }, cancellationToken);
    }
}
