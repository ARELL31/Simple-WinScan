using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;

namespace Simple_WinScan.Services.Implementations;

public class JsonSettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public JsonSettingsService() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinSimpleScan"))
    {
    }

    public JsonSettingsService(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("El directorio base no puede ser vacío.", nameof(baseDirectory));
        }

        Directory.CreateDirectory(baseDirectory);
        _settingsFilePath = Path.Combine(baseDirectory, "settings.json");
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            using var stream = File.OpenRead(_settingsFilePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions);
            return settings ?? new AppSettings();
        }
        catch
        {
            // Fallback ante archivo corrupto o inaccesible
            return new AppSettings();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        await _fileLock.WaitAsync();
        try
        {
            string tempPath = _settingsFilePath + ".tmp";
            using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions);
            }

            if (File.Exists(_settingsFilePath))
            {
                File.Replace(tempPath, _settingsFilePath, null);
            }
            else
            {
                File.Move(tempPath, _settingsFilePath);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
