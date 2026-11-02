using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Simple_WinScan.Helpers;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Implementations;
using Simple_WinScan.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Simple_WinScan.Tests;

public class ImageExportAndParityTests : IDisposable
{
    private readonly string _tempDir;

    public ImageExportAndParityTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SimpleWinScanTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    private static byte[] CreateSamplePngBytes(int width, int height, Rgba32 color)
    {
        using var img = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                img[x, y] = color;
            }
        }
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task ExportSinglePage_JpegPngWebp_GeneratesValidFiles()
    {
        var service = new ImageSharpExportService();
        var rawPng = CreateSamplePngBytes(100, 100, Color.CornflowerBlue);
        var page = new ScannedPage
        {
            ImageBytes = rawPng,
            PixelWidth = 100,
            PixelHeight = 100
        };

        string jpegPath = Path.Combine(_tempDir, "test_output.jpg");
        string pngPath = Path.Combine(_tempDir, "test_output.png");
        string webpPath = Path.Combine(_tempDir, "test_output.webp");

        var resJpeg = await service.ExportPagesAsync(new[] { page }, jpegPath, ImageExportFormat.Jpeg);
        var resPng = await service.ExportPagesAsync(new[] { page }, pngPath, ImageExportFormat.Png);
        var resWebp = await service.ExportPagesAsync(new[] { page }, webpPath, ImageExportFormat.WebP);

        Assert.Single(resJpeg);
        Assert.Single(resPng);
        Assert.Single(resWebp);

        Assert.True(File.Exists(jpegPath));
        Assert.True(File.Exists(pngPath));
        Assert.True(File.Exists(webpPath));

        // Verificar que los bytes de salida son decodificables
        using var loadedJpeg = Image.Load(jpegPath);
        Assert.Equal(100, loadedJpeg.Width);
        Assert.Equal(100, loadedJpeg.Height);

        using var loadedPng = Image.Load(pngPath);
        Assert.Equal(100, loadedPng.Width);
        Assert.Equal(100, loadedPng.Height);

        using var loadedWebp = Image.Load(webpPath);
        Assert.Equal(100, loadedWebp.Width);
        Assert.Equal(100, loadedWebp.Height);
    }

    [Fact]
    public async Task ExportBatch_MultiplePages_CreatesNumberedFiles()
    {
        var service = new ImageSharpExportService();
        var rawPng = CreateSamplePngBytes(80, 80, Color.DarkSeaGreen);

        var pages = new List<ScannedPage>
        {
            new() { ImageBytes = rawPng, PixelWidth = 80, PixelHeight = 80 },
            new() { ImageBytes = rawPng, PixelWidth = 80, PixelHeight = 80 },
            new() { ImageBytes = rawPng, PixelWidth = 80, PixelHeight = 80 }
        };

        string basePath = Path.Combine(_tempDir, "Factura.jpg");
        var results = await service.ExportPagesAsync(pages, basePath, ImageExportFormat.Jpeg);

        Assert.Equal(3, results.Count);
        Assert.Contains("Factura_1.jpg", results[0]);
        Assert.Contains("Factura_2.jpg", results[1]);
        Assert.Contains("Factura_3.jpg", results[2]);

        foreach (var file in results)
        {
            Assert.True(File.Exists(file));
        }
    }

    [Fact]
    public void ParityCheck_CropAndRotation_ProducesConsistentDimensions()
    {
        // Crear imagen 200x100
        var rawPng = CreateSamplePngBytes(200, 100, Color.Red);

        // Al rotar 90°, el canvas pasa a ser 100x200.
        // Recortar la mitad horizontal (Width=0.5) del canvas rotado produce Ancho=50, Alto=200.
        var crop = new CropRectangle(0.0, 0.0, 0.5, 1.0);
        byte[] processed = ImageHelper.ProcessImage(rawPng, 90, crop);

        using var img = Image.Load(processed);
        Assert.Equal(50, img.Width);
        Assert.Equal(200, img.Height);
    }
}
