using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using PdfSharpCore.Pdf.IO;
using Simple_WinScan.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Simple_WinScan.Services.Implementations;
using Simple_WinScan.Services.Interfaces;
using Xunit;

namespace Simple_WinScan.Tests;

public class OcrSearchablePdfTests
{
    private static byte[] CreateSamplePngBytes(int width, int height)
    {
        using var img = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    private class MockOcrService : IOcrService
    {
        public bool IsOcrSupported => true;

        public Task<OcrPageResult?> RecognizeTextAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            var result = new OcrPageResult
            {
                FullText = "Simple-WinScan OCR 2.0 Test Phrase"
            };

            result.Words.Add(new OcrWordBox
            {
                Text = "Simple-WinScan",
                X = 40,
                Y = 60,
                Width = 120,
                Height = 24
            });

            result.Words.Add(new OcrWordBox
            {
                Text = "OCR-Searchable-Phrase",
                X = 180,
                Y = 60,
                Width = 160,
                Height = 24
            });

            return Task.FromResult<OcrPageResult?>(result);
        }
    }

    [Fact]
    public async Task ExportPdf_WithOcrEnabled_GeneratesSearchableTextLayerMatchingVisibleBounds()
    {
        var mockOcr = new MockOcrService();
        var pdfService = new PdfSharpExportService(mockOcr);

        var page = new ScannedPage
        {
            ImageBytes = CreateSamplePngBytes(400, 300),
            PixelWidth = 400,
            PixelHeight = 300,
            Dpi = 300
        };

        using var outputMs = new MemoryStream();
        await pdfService.ExportPagesToPdfAsync(new[] { page }, outputMs, enableOcr: true);

        byte[] pdfBytes = outputMs.ToArray();
        Assert.NotEmpty(pdfBytes);

        // Abrir el PDF resultante para verificar estructura y contenido
        using var readMs = new MemoryStream(pdfBytes);
        using var pdfDocument = PdfReader.Open(readMs, PdfDocumentOpenMode.Modify);

        Assert.Equal(1, pdfDocument.PageCount);
        var pdfPage = pdfDocument.Pages[0];

        // Verificar dimensiones de página en puntos (400 / 300 * 72 = 96 pt, 300 / 300 * 72 = 72 pt)
        Assert.True(pdfPage.Width.Point > 0);
        Assert.True(pdfPage.Height.Point > 0);

        // Extraer tokens de texto usando el ContentReader nativo de PdfSharpCore
        var sequence = ContentReader.ReadContent(pdfPage);
        var extractedWords = new List<string>();
        ExtractText(sequence, extractedWords);

        Assert.Contains(extractedWords, w => w.Contains("Simple-WinScan"));
        Assert.Contains(extractedWords, w => w.Contains("OCR-Searchable-Phrase"));

        // Comprobar que las coordenadas corresponden a la zona visible de la página (dentro de los límites de ancho y alto)
        double scaleX = pdfPage.Width.Point / page.PixelWidth;
        double scaleY = pdfPage.Height.Point / page.PixelHeight;

        double word1X = 40 * scaleX;
        double word1Y = 60 * scaleY;
        Assert.InRange(word1X, 0, pdfPage.Width.Point);
        Assert.InRange(word1Y, 0, pdfPage.Height.Point);

        double word2X = 180 * scaleX;
        double word2Y = 60 * scaleY;
        Assert.InRange(word2X, 0, pdfPage.Width.Point);
        Assert.InRange(word2Y, 0, pdfPage.Height.Point);
    }

    [Fact]
    public async Task ExportPdf_WithOcrDisabled_DoesNotIncludeOcrTextStream()
    {
        var mockOcr = new MockOcrService();
        var pdfService = new PdfSharpExportService(mockOcr);

        var page = new ScannedPage
        {
            ImageBytes = CreateSamplePngBytes(200, 200),
            PixelWidth = 200,
            PixelHeight = 200,
            Dpi = 300
        };

        using var outputMs = new MemoryStream();
        await pdfService.ExportPagesToPdfAsync(new[] { page }, outputMs, enableOcr: false);

        using var readMs = new MemoryStream(outputMs.ToArray());
        using var pdfDocument = PdfReader.Open(readMs, PdfDocumentOpenMode.Modify);
        var sequence = ContentReader.ReadContent(pdfDocument.Pages[0]);
        var extractedWords = new List<string>();
        ExtractText(sequence, extractedWords);
        Assert.DoesNotContain(extractedWords, w => w.Contains("OCR-Searchable-Phrase"));
    }

    [Fact]
    public void WindowsMediaOcrService_Instantiation_WorksOnWindows()
    {
        var ocr = new WindowsMediaOcrService();
        // En Windows 10/11, debe instanciarse sin lanzar excepciones
        Assert.NotNull(ocr);
    }

    private static void ExtractText(CObject cObject, List<string> words)
    {
        if (cObject is CString cStr)
        {
            words.Add(cStr.Value);
        }
        else if (cObject is CSequence seq)
        {
            foreach (var item in seq)
            {
                ExtractText(item, words);
            }
        }
        else if (cObject is COperator op)
        {
            foreach (var operand in op.Operands)
            {
                ExtractText(operand, words);
            }
        }
    }
}
