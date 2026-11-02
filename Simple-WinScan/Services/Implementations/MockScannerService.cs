using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Simple_WinScan.Services.Implementations;

/// <summary>
/// Proveedor independiente para pruebas y simulación sin hardware físico.
/// Solo se activa por elección explícita del usuario.
/// </summary>
public class MockScannerService : IScannerService
{
    public const string MockScannerId = "MOCK_SCANNER_001";
    public const string MockScannerName = "Escáner Virtual (Modo Simulación)";

    public bool IsMockMode => true;

    public static ScannerDeviceInfo CreateMockDeviceInfo() => new()
    {
        Id = MockScannerId,
        Name = MockScannerName,
        Description = "Generador de escaneos de alta calidad para pruebas sin hardware",
        Manufacturer = "WinSimpleScan",
        IsDefault = false
    };

    public Task<IReadOnlyList<ScannerDeviceInfo>> GetAvailableScannersAsync()
    {
        IReadOnlyList<ScannerDeviceInfo> list = new List<ScannerDeviceInfo> { CreateMockDeviceInfo() };
        return Task.FromResult(list);
    }

    public Task<ScannerCapabilities> GetCapabilitiesAsync(ScannerDeviceInfo scanner)
    {
        return Task.FromResult(ScannerCapabilities.Default);
    }

    public async Task<ScannedPage> ScanPageAsync(
        ScannerDeviceInfo? scanner,
        ScanProfile profile,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Simular progreso cuantitativo progresivo y latencia de adquisición
        for (int p = 0; p <= 100; p += 20)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(p);
            await Task.Delay(120, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return GenerateMockPage(profile);
    }

    public int SimulatedAdfPageCount { get; set; } = 3;
    public int? FailOnAdfPageNumber { get; set; } = null;

    public async Task<IReadOnlyList<ScannedPage>> ScanBatchAsync(
        ScannerDeviceInfo? scanner,
        ScanProfile profile,
        Action<ScannedPage> onPageAcquired,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var acquiredPages = new List<ScannedPage>();
        int count = profile.Source == ScanSource.Flatbed ? 1 : Math.Max(1, SimulatedAdfPageCount);

        for (int i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (FailOnAdfPageNumber.HasValue && i == FailOnAdfPageNumber.Value)
            {
                throw new Exceptions.ScannerHardwareException(
                    "Atasco de papel en alimentador automático (ADF).",
                    unchecked((int)0x80210002),
                    scanner?.Id ?? MockScannerId,
                    scanner?.Name ?? MockScannerName);
            }

            var page = await ScanPageAsync(scanner, profile, progress, cancellationToken);
            acquiredPages.Add(page);
            onPageAcquired?.Invoke(page);
        }

        return acquiredPages;
    }

    public static ScannedPage GenerateMockPage(ScanProfile profile)
    {
        int dpi = profile.Dpi;
        int width = (int)(8.27 * dpi);
        int height = (int)(11.69 * dpi);

        using var image = new Image<Rgba32>(width, height);

        Rgba32 paperBg = profile.ColorMode switch
        {
            ScanColorMode.Color => new Rgba32(253, 252, 248),
            _ => new Rgba32(255, 255, 255)
        };

        Rgba32 inkColor = profile.ColorMode switch
        {
            ScanColorMode.Color => new Rgba32(18, 48, 88),
            ScanColorMode.Grayscale => new Rgba32(40, 40, 40),
            _ => new Rgba32(0, 0, 0)
        };

        Rgba32 secondaryInk = profile.ColorMode switch
        {
            ScanColorMode.Color => new Rgba32(120, 140, 160),
            _ => new Rgba32(150, 150, 150)
        };

        void FillRect(int startX, int startY, int rectW, int rectH, Rgba32 color)
        {
            int endY = Math.Min(startY + rectH, height);
            int endX = Math.Min(startX + rectW, width);

            for (int y = Math.Max(0, startY); y < endY; y++)
            {
                for (int x = Math.Max(0, startX); x < endX; x++)
                {
                    image[x, y] = color;
                }
            }
        }

        // Fondo del documento
        FillRect(0, 0, width, height, paperBg);

        int marginX = (int)(0.8 * dpi);
        int marginY = (int)(0.8 * dpi);

        // Encabezado
        FillRect(marginX, marginY, (int)(4.0 * dpi), (int)(0.22 * dpi), inkColor);
        FillRect(marginX, marginY + (int)(0.35 * dpi), (int)(2.5 * dpi), (int)(0.08 * dpi), secondaryInk);

        // Separador horizontal
        FillRect(marginX, marginY + (int)(0.6 * dpi), width - (2 * marginX), 4, secondaryInk);

        // Párrafos simulados
        int currentY = marginY + (int)(0.9 * dpi);
        for (int p = 0; p < 7; p++)
        {
            int linesInParagraph = 4 + (p % 3);
            for (int line = 0; line < linesInParagraph; line++)
            {
                int lineWidth = (line == linesInParagraph - 1)
                    ? (int)((3.0 + (p % 2)) * dpi)
                    : width - (2 * marginX);

                FillRect(marginX, currentY, lineWidth, (int)(0.045 * dpi), inkColor);
                currentY += (int)(0.11 * dpi);
            }
            currentY += (int)(0.25 * dpi);
        }

        // Marca de agua sutil en pie de página indicando modo simulación
        FillRect(marginX, height - marginY, (int)(3.5 * dpi), 2, secondaryInk);

        using var memoryStream = new MemoryStream();
        image.SaveAsPng(memoryStream);

        return new ScannedPage
        {
            ImageBytes = memoryStream.ToArray(),
            PixelWidth = width,
            PixelHeight = height,
            Dpi = dpi,
            RotationAngle = 0
        };
    }
}
