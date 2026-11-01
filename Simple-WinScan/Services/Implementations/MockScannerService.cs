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

    public async Task<ScannedPage> ScanPageAsync(ScannerDeviceInfo? scanner, ScanProfile profile, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Simular latencia realista de escaneo (1.5 segundos)
        await Task.Delay(1500, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return GenerateMockPage(profile);
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
                var rowSpan = image.GetPixelRowSpan(y);
                for (int x = Math.Max(0, startX); x < endX; x++)
                {
                    rowSpan[x] = color;
                }
            }
        }

        // Fondo
        FillRect(0, 0, width, height, paperBg);

        // Borde decorativo
        FillRect(20, 20, width - 40, 2, secondaryInk);
        FillRect(20, height - 22, width - 40, 2, secondaryInk);
        FillRect(20, 20, 2, height - 40, secondaryInk);
        FillRect(width - 22, 20, 2, height - 40, secondaryInk);

        // Encabezado
        FillRect(60, 60, width - 120, 32, inkColor);

        // Marca de agua o badge de simulación
        FillRect(60, 105, 300, 16, secondaryInk);

        // Bloques de texto simulados
        int currentY = 160;
        var rand = new Random(42);
        while (currentY < height - 150)
        {
            int lineWidth = rand.Next(width - 240, width - 130);
            FillRect(60, currentY, lineWidth, 10, inkColor);
            currentY += 26;

            if (rand.NextDouble() > 0.85)
            {
                currentY += 18;
            }
        }

        // Pie de página
        FillRect(60, height - 80, 180, 8, secondaryInk);
        FillRect(width - 160, height - 80, 100, 8, secondaryInk);

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);

        return new ScannedPage
        {
            ImageBytes = ms.ToArray(),
            PixelWidth = width,
            PixelHeight = height,
            Dpi = profile.Dpi,
            RotationAngle = 0
        };
    }
}
