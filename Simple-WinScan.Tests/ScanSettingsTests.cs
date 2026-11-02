using System.Threading.Tasks;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Implementations;
using Simple_WinScan.Services.Interfaces;
using Simple_WinScan.ViewModels;
using Xunit;

namespace Simple_WinScan.Tests;

public class ScanSettingsTests
{
    private class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; set; } = new();
        public Task<AppSettings> LoadSettingsAsync() => Task.FromResult(CurrentSettings);
        public Task SaveSettingsAsync(AppSettings settings)
        {
            CurrentSettings = settings;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void ScannerCapabilities_DefaultValues_AreSensible()
    {
        var caps = ScannerCapabilities.Default;

        Assert.NotEmpty(caps.SupportedDpis);
        Assert.Contains(300, caps.SupportedDpis);
        Assert.Contains(600, caps.SupportedDpis);
        Assert.True(caps.SupportsBrightness);
        Assert.True(caps.SupportsContrast);
        Assert.Contains(PageSizeFormat.A4, caps.SupportedPaperSizes);
        Assert.Contains(PageSizeFormat.Letter, caps.SupportedPaperSizes);
    }

    [Fact]
    public async Task ScanSettingsViewModel_LoadAndSave_UpdatesSettingsAndAppliesToProfile()
    {
        var settingsService = new FakeSettingsService();
        settingsService.CurrentSettings.Brightness = 25;
        settingsService.CurrentSettings.Contrast = -15;
        settingsService.CurrentSettings.CustomDpi = 600;
        settingsService.CurrentSettings.PaperSize = PageSizeFormat.Letter;
        settingsService.CurrentSettings.ContinuousScanDelaySeconds = 7;

        var vm = new ScanSettingsViewModel(settingsService, ScannerCapabilities.Default);
        await vm.InitializeAsync();

        Assert.Equal(25, vm.Brightness);
        Assert.Equal(-15, vm.Contrast);
        Assert.Equal(600, vm.SelectedDpi);
        Assert.Equal(PageSizeFormat.Letter, vm.SelectedPaperSize);
        Assert.Equal(7, vm.ContinuousDelaySeconds);

        // Modificar valores
        vm.Brightness = 10;
        vm.Contrast = 20;
        vm.SelectedDpi = 300;
        vm.SelectedPaperSize = PageSizeFormat.A4;
        vm.ContinuousDelaySeconds = 4;

        await vm.SaveAsync();

        Assert.Equal(10, settingsService.CurrentSettings.Brightness);
        Assert.Equal(20, settingsService.CurrentSettings.Contrast);
        Assert.Equal(300, settingsService.CurrentSettings.CustomDpi);
        Assert.Equal(PageSizeFormat.A4, settingsService.CurrentSettings.PaperSize);
        Assert.Equal(4, settingsService.CurrentSettings.ContinuousScanDelaySeconds);
    }
}
