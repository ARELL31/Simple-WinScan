using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;
using SixLabors.ImageSharp;

namespace Simple_WinScan.Services.Implementations;

public class CommandPostprocessingService : IPostprocessingService
{
    public async Task<ScannedPage> ProcessPageAsync(ScannedPage page, string commandTemplate, CancellationToken cancellationToken = default)
    {
        if (page == null) throw new ArgumentNullException(nameof(page));
        if (string.IsNullOrWhiteSpace(commandTemplate)) return page;

        // Conservar siempre la imagen original antes de transformar
        if (page.OriginalImageBytes == null || page.OriginalImageBytes.Length == 0)
        {
            page.OriginalImageBytes = (byte[])page.ImageBytes.Clone();
        }

        string tempInput = Path.Combine(Path.GetTempPath(), $"sws_pp_in_{Guid.NewGuid():N}.png");
        string tempOutput = Path.Combine(Path.GetTempPath(), $"sws_pp_out_{Guid.NewGuid():N}.png");

        try
        {
            await File.WriteAllBytesAsync(tempInput, page.ImageBytes, cancellationToken);

            string command = commandTemplate
                .Replace("{input}", $"\"{tempInput}\"", StringComparison.OrdinalIgnoreCase)
                .Replace("{output}", $"\"{tempOutput}\"", StringComparison.OrdinalIgnoreCase)
                .Replace("{dpi}", page.Dpi.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{rotation}", page.RotationAngle.ToString(), StringComparison.OrdinalIgnoreCase);

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && File.Exists(tempOutput))
            {
                byte[] outputBytes = await File.ReadAllBytesAsync(tempOutput, cancellationToken);
                if (outputBytes.Length > 0)
                {
                    using var img = Image.Load(outputBytes);
                    page.ImageBytes = outputBytes;
                    page.PixelWidth = img.Width;
                    page.PixelHeight = img.Height;
                }
            }
            else
            {
                string error = await process.StandardError.ReadToEndAsync(cancellationToken);
                throw new InvalidOperationException($"El comando de posprocesamiento finalizó con código {process.ExitCode}: {error}");
            }

            return page;
        }
        finally
        {
            try
            {
                if (File.Exists(tempInput)) File.Delete(tempInput);
                if (File.Exists(tempOutput)) File.Delete(tempOutput);
            }
            catch { }
        }
    }

    public Task<bool> RevertToOriginalAsync(ScannedPage page)
    {
        if (page == null) return Task.FromResult(false);

        if (page.OriginalImageBytes != null && page.OriginalImageBytes.Length > 0)
        {
            page.ImageBytes = (byte[])page.OriginalImageBytes.Clone();
            try
            {
                using var img = Image.Load(page.ImageBytes);
                page.PixelWidth = img.Width;
                page.PixelHeight = img.Height;
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }
}
