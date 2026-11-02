using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;
using Simple_WinScan.ViewModels;

namespace Simple_WinScan.Services.Implementations;

public class JsonAutosaveService : IAutosaveService
{
    private readonly string _autosaveDirectory;
    private readonly string _sessionFilePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public JsonAutosaveService(string? customDirectory = null)
    {
        _autosaveDirectory = customDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinSimpleScan",
            "Autosaves");
        _sessionFilePath = Path.Combine(_autosaveDirectory, "session.json");
    }

    public async Task<bool> HasAutosavedSessionAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(_sessionFilePath)) return false;

            string json = await File.ReadAllTextAsync(_sessionFilePath);
            var session = JsonSerializer.Deserialize<AutosaveSession>(json);
            return session != null && session.Pages.Count > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<ScannedPage>> RecoverSessionAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(_sessionFilePath)) return Array.Empty<ScannedPage>();

            string json = await File.ReadAllTextAsync(_sessionFilePath);
            var session = JsonSerializer.Deserialize<AutosaveSession>(json);
            if (session == null || session.Pages.Count == 0) return Array.Empty<ScannedPage>();

            var recovered = new List<ScannedPage>();
            foreach (var meta in session.Pages)
            {
                string imgPath = Path.Combine(_autosaveDirectory, meta.ImageFileName);
                if (!File.Exists(imgPath)) continue;

                byte[] bytes = await File.ReadAllBytesAsync(imgPath);
                var page = new ScannedPage
                {
                    ImageBytes = bytes,
                    RotationAngle = meta.RotationAngle,
                    Dpi = meta.Dpi,
                    Crop = new CropRectangle(meta.CropX, meta.CropY, meta.CropWidth, meta.CropHeight)
                };
                recovered.Add(page);
            }

            return recovered;
        }
        catch
        {
            return Array.Empty<ScannedPage>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveSessionAsync(IReadOnlyList<ScannedPageItemViewModel> pages)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (pages == null || pages.Count == 0)
            {
                InternalClear();
                return;
            }

            Directory.CreateDirectory(_autosaveDirectory);

            var session = new AutosaveSession();

            for (int i = 0; i < pages.Count; i++)
            {
                var item = pages[i];
                string fileName = $"page_{i + 1}.dat";
                string fullPath = Path.Combine(_autosaveDirectory, fileName);

                await File.WriteAllBytesAsync(fullPath, item.Model.ImageBytes);

                var meta = new AutosavePageMetadata
                {
                    PageNumber = item.PageNumber,
                    RotationAngle = item.RotationAngle,
                    Dpi = item.Model.Dpi,
                    ImageFileName = fileName
                };

                if (item.Crop != null)
                {
                    meta.CropX = item.Crop.X;
                    meta.CropY = item.Crop.Y;
                    meta.CropWidth = item.Crop.Width;
                    meta.CropHeight = item.Crop.Height;
                }

                session.Pages.Add(meta);
            }

            string json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_sessionFilePath, json);
        }
        catch
        {
            // Silencioso para no interrumpir el flujo del usuario si hay error de I/O en autoguardado
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearSessionAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            InternalClear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void InternalClear()
    {
        try
        {
            if (Directory.Exists(_autosaveDirectory))
            {
                Directory.Delete(_autosaveDirectory, true);
            }
        }
        catch { }
    }
}
