using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Simple_WinScan.Helpers;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;

namespace Simple_WinScan.Services.Implementations;

public class PdfSharpExportService : IPdfExportService
{
    private readonly IOcrService? _ocrService;

    public PdfSharpExportService(IOcrService? ocrService = null)
    {
        _ocrService = ocrService;
    }

    public Task ExportPagesToPdfAsync(IReadOnlyList<ScannedPage> pages, Stream outputStream, CancellationToken cancellationToken = default)
    {
        return ExportPagesToPdfAsync(pages, outputStream, false, cancellationToken);
    }

    public Task ExportPagesToPdfAsync(IReadOnlyList<ScannedPage> pages, Stream outputStream, bool enableOcr, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (pages == null || pages.Count == 0)
            {
                throw new ArgumentException("No hay páginas para exportar.", nameof(pages));
            }

            using var document = new PdfDocument();
            document.Info.Title = "Documento Escaneado - WinSimpleScan";
            document.Info.Author = "ARELL31 (WinSimpleScan)";
            document.Info.CreationDate = DateTime.Now;

            foreach (var page in pages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Procesar bytes aplicando rotación y recorte para paridad exacta
                byte[] finalBytes = ImageHelper.ProcessImage(page.ImageBytes, page.RotationAngle, page.Crop);

                using var imageStream = new MemoryStream(finalBytes);
                using var xImage = XImage.FromStream(() => new MemoryStream(finalBytes));

                // Calcular tamaño de página en puntos tipográficos (72 pt = 1 pulgada)
                double dpi = page.Dpi > 0 ? page.Dpi : 300.0;
                double widthPoints = (xImage.PixelWidth / dpi) * 72.0;
                double heightPoints = (xImage.PixelHeight / dpi) * 72.0;

                var pdfPage = document.AddPage();
                pdfPage.Width = XUnit.FromPoint(widthPoints);
                pdfPage.Height = XUnit.FromPoint(heightPoints);

                using var gfx = XGraphics.FromPdfPage(pdfPage);
                gfx.DrawImage(xImage, 0, 0, widthPoints, heightPoints);

                if (enableOcr && _ocrService != null && _ocrService.IsOcrSupported)
                {
                    try
                    {
                        var ocrResult = _ocrService.RecognizeTextAsync(finalBytes, cancellationToken).GetAwaiter().GetResult();
                        if (ocrResult != null && ocrResult.Words.Count > 0)
                        {
                            double scaleX = widthPoints / xImage.PixelWidth;
                            double scaleY = heightPoints / xImage.PixelHeight;

                            var transparentBrush = new XSolidBrush(XColor.FromArgb(0, 0, 0, 0));

                            foreach (var word in ocrResult.Words)
                            {
                                double wordX = word.X * scaleX;
                                double wordY = word.Y * scaleY;
                                double wordW = word.Width * scaleX;
                                double wordH = word.Height * scaleY;

                                double fontSize = Math.Max(6, wordH * 0.85);
                                var fontOptions = new XPdfFontOptions(PdfFontEncoding.WinAnsi);
                                var font = new XFont("Arial", fontSize, XFontStyle.Regular, fontOptions);

                                var rect = new XRect(wordX, wordY, Math.Max(wordW, 20), Math.Max(wordH, fontSize));
                                gfx.DrawString(word.Text, font, transparentBrush, rect, XStringFormats.TopLeft);
                            }
                        }
                    }
                    catch
                    {
                        // Si falla OCR para una página, continúa conservando la imagen sin error fatal
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            document.Save(outputStream, false);
        }, cancellationToken);
    }
}
