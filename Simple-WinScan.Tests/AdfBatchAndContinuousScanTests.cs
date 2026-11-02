using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Exceptions;
using Simple_WinScan.Services.Implementations;
using Simple_WinScan.Services.Interfaces;
using Simple_WinScan.ViewModels;
using Xunit;

namespace Simple_WinScan.Tests;

public class AdfBatchAndContinuousScanTests
{
    private class FakeBatchScannerService : IScannerService
    {
        public bool IsMockMode => false;
        public int AvailablePagesInFeeder { get; set; } = 3;
        public int? JamAtPage { get; set; } = null;

        public Task<IReadOnlyList<ScannerDeviceInfo>> GetAvailableScannersAsync()
        {
            var list = new List<ScannerDeviceInfo>
            {
                new() { Id = "ADF_DEV", Name = "Escáner ADF Multifunción", IsDefault = true }
            };
            return Task.FromResult<IReadOnlyList<ScannerDeviceInfo>>(list);
        }

        public Task<ScannerCapabilities> GetCapabilitiesAsync(ScannerDeviceInfo scanner)
        {
            return Task.FromResult(ScannerCapabilities.Default);
        }

        public Task<ScannedPage> ScanPageAsync(
            ScannerDeviceInfo? scanner,
            ScanProfile profile,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ScannedPage
            {
                ImageBytes = new byte[] { 1, 2, 3 },
                PixelWidth = 200,
                PixelHeight = 300,
                Dpi = profile.Dpi
            });
        }

        public async Task<IReadOnlyList<ScannedPage>> ScanBatchAsync(
            ScannerDeviceInfo? scanner,
            ScanProfile profile,
            Action<ScannedPage> onPageAcquired,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var pages = new List<ScannedPage>();
            for (int i = 1; i <= AvailablePagesInFeeder; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (JamAtPage.HasValue && i == JamAtPage.Value)
                {
                    throw new ScannerHardwareException("Atasco de papel en alimentador automático (ADF).", unchecked((int)0x80210002));
                }

                var page = await ScanPageAsync(scanner, profile, progress, cancellationToken);
                pages.Add(page);
                onPageAcquired?.Invoke(page);
            }
            return pages;
        }
    }

    private class FakePdfExportService : IPdfExportService
    {
        public Task ExportPagesToPdfAsync(IReadOnlyList<ScannedPage> pages, Stream outputStream, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class FakeDialogService : IDialogService
    {
        public Task<string?> PickSavePdfPathAsync(string defaultFileName) => Task.FromResult<string?>("test.pdf");
        public Task<string?> PickSaveImagePathAsync(string defaultFileName, ImageExportFormat format) => Task.FromResult<string?>("test.jpg");
        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;
        public Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText) => Task.FromResult(true);
    }

    private class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; set; } = new();
        public Task<AppSettings> LoadSettingsAsync() => Task.FromResult(CurrentSettings);
        public Task SaveSettingsAsync(AppSettings settings) => Task.CompletedTask;
    }

    [Fact]
    public async Task ScanBatchAsync_WhenFeederHas3Pages_AcquiresAll3Pages()
    {
        var fakeScanner = new FakeBatchScannerService { AvailablePagesInFeeder = 3 };
        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService(), new FakeSettingsService());
        await vm.InitializeAsync();

        var profile = ScanProfile.TextDefault with { Source = ScanSource.Feeder };
        vm.SelectedProfile = profile;

        await vm.ScanBatchAdfAsync();

        Assert.Equal(3, vm.Pages.Count);
        Assert.False(vm.IsScanning);
        Assert.True(vm.CanScan);
    }

    [Fact]
    public async Task ScanBatchAsync_WhenPaperJamAtPage3_PreservesFirst2PagesAndShowsWarning()
    {
        var fakeScanner = new FakeBatchScannerService
        {
            AvailablePagesInFeeder = 5,
            JamAtPage = 3
        };
        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService(), new FakeSettingsService());
        await vm.InitializeAsync();

        var profile = ScanProfile.TextDefault with { Source = ScanSource.Feeder };
        vm.SelectedProfile = profile;

        await vm.ScanBatchAdfAsync();

        // Las 2 primeras páginas deben haberse conservado en el lienzo
        Assert.Equal(2, vm.Pages.Count);
        Assert.False(vm.IsScanning);
        Assert.True(vm.CanScan);
        Assert.True(vm.IsInfoBarOpen);
        Assert.Equal(InfoBarSeverity.Warning, vm.InfoBarSeverity);
        Assert.Contains("atasco", vm.InfoBarMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2 páginas", vm.InfoBarMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ContinuousScan_WhenStoppedByUser_PreservesScannedPages()
    {
        var fakeScanner = new FakeBatchScannerService { AvailablePagesInFeeder = 10 };
        var settings = new FakeSettingsService
        {
            CurrentSettings = new AppSettings { ContinuousScanDelaySeconds = 1 }
        };
        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService(), settings);
        await vm.InitializeAsync();

        // Iniciar modo continuo y cancelar tras 1 página
        var scanTask = vm.StartContinuousScanAsync();
        // Esperar a que la primera página sea capturada
        await Task.Delay(150);
        vm.StopContinuousScan();
        await scanTask;

        Assert.True(vm.Pages.Count >= 1);
        Assert.False(vm.IsContinuousScanActive);
        Assert.False(vm.IsScanning);
    }
}
