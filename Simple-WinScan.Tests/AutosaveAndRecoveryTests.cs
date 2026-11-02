using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Simple_WinScan.Services.Implementations;
using Simple_WinScan.Services.Interfaces;
using Simple_WinScan.ViewModels;
using Xunit;

namespace Simple_WinScan.Tests;

public class AutosaveAndRecoveryTests : IDisposable
{
    private readonly string _testAutosaveDir;

    public AutosaveAndRecoveryTests()
    {
        _testAutosaveDir = Path.Combine(Path.GetTempPath(), "SimpleWinScanAutosaveTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testAutosaveDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testAutosaveDir))
            {
                Directory.Delete(_testAutosaveDir, true);
            }
        }
        catch { }
    }

    private static byte[] CreateSamplePngBytes(int width, int height)
    {
        using var img = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task JsonAutosaveService_SaveAndRecover_RestoresExactProperties()
    {
        var service = new JsonAutosaveService(_testAutosaveDir);
        Assert.False(await service.HasAutosavedSessionAsync());

        var page1 = new ScannedPage
        {
            ImageBytes = CreateSamplePngBytes(100, 100),
            RotationAngle = 90,
            Crop = new CropRectangle(0.1, 0.1, 0.8, 0.8),
            Dpi = 300
        };
        var vm1 = new ScannedPageItemViewModel(page1, 1);

        var page2 = new ScannedPage
        {
            ImageBytes = CreateSamplePngBytes(120, 120),
            RotationAngle = 180,
            Dpi = 150
        };
        var vm2 = new ScannedPageItemViewModel(page2, 2);

        await service.SaveSessionAsync(new[] { vm1, vm2 });

        Assert.True(await service.HasAutosavedSessionAsync());

        var recovered = await service.RecoverSessionAsync();
        Assert.Equal(2, recovered.Count);

        Assert.Equal(90, recovered[0].RotationAngle);
        Assert.NotNull(recovered[0].Crop);
        Assert.Equal(0.1, recovered[0].Crop!.X, precision: 2);
        Assert.Equal(0.8, recovered[0].Crop!.Width, precision: 2);

        Assert.Equal(180, recovered[1].RotationAngle);
        Assert.Equal(150, recovered[1].Dpi);

        await service.ClearSessionAsync();
        Assert.False(await service.HasAutosavedSessionAsync());
    }

    [Fact]
    public async Task MainViewModel_IsDirtyAndRecoveryFlow()
    {
        var fakeScanner = new MockScannerService();
        var fakePdf = new PdfSharpExportService();
        var fakeImage = new ImageSharpExportService();
        var fakePrint = new SystemDrawingPrintService();
        var fakeSystem = new FakeSystemService();
        var autosaveService = new JsonAutosaveService(_testAutosaveDir);
        var fakeDialog = new FakeDialogService();
        var fakeSettings = new FakeSettingsService();

        var vm = new MainViewModel(fakeScanner, fakeScanner, fakePdf, fakeImage, fakePrint, fakeSystem, autosaveService, fakeDialog, fakeSettings);

        Assert.False(vm.IsDirty);

        var page = new ScannedPage
        {
            ImageBytes = CreateSamplePngBytes(100, 100),
            PixelWidth = 100,
            PixelHeight = 100
        };
        await vm.AddAcquiredPageAsync(page);

        Assert.True(vm.IsDirty);

        // Confirmar cierre con diálogo
        bool canClose = await vm.ConfirmCloseIfDirtyAsync();
        Assert.True(canClose);
        Assert.False(vm.IsDirty);
    }

    private class FakeSystemService : ISystemIntegrationService
    {
        public Task<bool> OpenInExternalViewerAsync(ScannedPage page) => Task.FromResult(true);
        public Task<bool> OpenDocumentInExternalViewerAsync(IReadOnlyList<ScannedPage> pages) => Task.FromResult(true);
        public Task<bool> EmailPagesAsync(IReadOnlyList<ScannedPage> pages, string subject = "Documento Escaneado") => Task.FromResult(true);
        public Task<bool> SharePagesAsync(IReadOnlyList<ScannedPage> pages, string title = "Documento Escaneado") => Task.FromResult(true);
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
}
