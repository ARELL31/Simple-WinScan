using System;
using System.IO;
using System.Threading.Tasks;
using Simple_WinScan.Services.Implementations;
using Xunit;

namespace Simple_WinScan.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _testDirectory;

    public SettingsServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "WinSimpleScan_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup failure in temp folder
        }
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        var service = new JsonSettingsService(_testDirectory);

        var settings = await service.LoadSettingsAsync();

        Assert.NotNull(settings);
        Assert.Null(settings.LastSelectedScannerId);
        Assert.False(settings.IsVirtualScannerEnabled);
        Assert.Equal(5, settings.ContinuousScanDelaySeconds);
    }

    [Fact]
    public async Task SaveAndLoadSettingsAsync_PersistsValuesCorrectly()
    {
        var service = new JsonSettingsService(_testDirectory);

        var settings = await service.LoadSettingsAsync();
        settings.LastSelectedScannerId = "Scanner_USB_001";
        settings.IsVirtualScannerEnabled = true;
        settings.ContinuousScanDelaySeconds = 8;
        settings.Brightness = 15;
        settings.Contrast = -10;

        await service.SaveSettingsAsync(settings);

        // Load with a new instance
        var reloadedService = new JsonSettingsService(_testDirectory);
        var reloadedSettings = await reloadedService.LoadSettingsAsync();

        Assert.Equal("Scanner_USB_001", reloadedSettings.LastSelectedScannerId);
        Assert.True(reloadedSettings.IsVirtualScannerEnabled);
        Assert.Equal(8, reloadedSettings.ContinuousScanDelaySeconds);
        Assert.Equal(15, reloadedSettings.Brightness);
        Assert.Equal(-10, reloadedSettings.Contrast);
    }
}
