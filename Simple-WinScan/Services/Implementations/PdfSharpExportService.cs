using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Simple_WinScan.Services.Implementations;

public class PdfSharpExportService : IPdfExportService
{
    public Task ExportPagesToPdfAsync(IReadOnlyList<ScannedPage> pages, Stream outputStream, CancellationToken cancellationToken = default)
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

                // Procesar bytes de imagen con la rotación si es diferente de 0
                byte[] finalBytes = page.ImageBytes;
                int currentRotation = page.RotationAngle % 360;

                if (currentRotation != 0)
                {
                    using var img = Image.Load(page.ImageBytes);
                    switch (currentRotation)
                    {
                        case 90:
                            img.Mutate(x => x.Rotate(90));
                            break;
                        case 180:
                            img.Mutate(x => x.Rotate(180));
                            break;
                        case 270:
                            img.Mutate(x => x.Rotate(270));
                            break;
                    }

                    using var rotatedMs = new MemoryStream();
                    img.SaveAsPng(rotatedMs);
                    finalBytes = rotatedMs.ToArray();
                }

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
            }

            cancellationToken.ThrowIfCancellationRequested();
            document.Save(outputStream, false);
        }, cancellationToken);
    }
}
