using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Helpers;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace Simple_WinScan.Services.Implementations;

public class ImageSharpExportService : IImageExportService
{
    public static string GetDefaultExtension(ImageExportFormat format) => format switch
    {
        ImageExportFormat.Jpeg => ".jpg",
        ImageExportFormat.Png => ".png",
        ImageExportFormat.WebP => ".webp",
        _ => ".jpg"
    };

    public static IImageEncoder CreateEncoder(ImageExportFormat format, int quality = 90) => format switch
    {
        ImageExportFormat.Jpeg => new JpegEncoder { Quality = Math.Clamp(quality, 1, 100) },
        ImageExportFormat.Png => new PngEncoder(),
        ImageExportFormat.WebP => new WebpEncoder { Quality = Math.Clamp(quality, 1, 100) },
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
    };

    public Task ExportPageAsync(ScannedPage page, Stream outputStream, ImageExportFormat format, int quality = 90, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var encoder = CreateEncoder(format, quality);
            byte[] processed = ImageHelper.ProcessImage(page.ImageBytes, page.RotationAngle, page.Crop, encoder);
            cancellationToken.ThrowIfCancellationRequested();
            outputStream.Write(processed, 0, processed.Length);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ExportPagesAsync(IReadOnlyList<ScannedPage> pages, string baseFilePath, ImageExportFormat format, int quality = 90, CancellationToken cancellationToken = default)
    {
        if (pages == null || pages.Count == 0)
        {
            throw new ArgumentException("No hay páginas para exportar.", nameof(pages));
        }

        var encoder = CreateEncoder(format, quality);
        var ext = Path.GetExtension(baseFilePath);
        if (string.IsNullOrEmpty(ext))
        {
            ext = GetDefaultExtension(format);
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(baseFilePath)) ?? "";
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var nameWithoutExt = Path.GetFileNameWithoutExtension(baseFilePath);

        if (pages.Count == 1)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string targetPath = Path.Combine(dir, $"{nameWithoutExt}{ext}");
            byte[] bytes = await Task.Run(() => ImageHelper.ProcessImage(pages[0].ImageBytes, pages[0].RotationAngle, pages[0].Crop, encoder), cancellationToken);
            await File.WriteAllBytesAsync(targetPath, bytes, cancellationToken);
            return new List<string> { targetPath };
        }

        var results = new List<string>(pages.Count);
        int digits = pages.Count >= 100 ? 3 : (pages.Count >= 10 ? 2 : 1);

        for (int i = 0; i < pages.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string indexStr = (i + 1).ToString().PadLeft(digits, '0');
            string outPath = Path.Combine(dir, $"{nameWithoutExt}_{indexStr}{ext}");

            byte[] bytes = await Task.Run(() => ImageHelper.ProcessImage(pages[i].ImageBytes, pages[i].RotationAngle, pages[i].Crop, encoder), cancellationToken);
            await File.WriteAllBytesAsync(outPath, bytes, cancellationToken);
            results.Add(outPath);
        }

        return results;
    }
}
