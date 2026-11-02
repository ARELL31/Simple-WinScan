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

public class PostprocessingTests
{
    private static byte[] CreateSamplePngBytes(int width, int height)
    {
        using var img = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task ProcessPageAsync_WithValidCopyCommand_PreservesOriginalAndUpdatesImage()
    {
        var service = new CommandPostprocessingService();
        var rawBytes = CreateSamplePngBytes(64, 64);
        var page = new ScannedPage
        {
            ImageBytes = rawBytes,
            PixelWidth = 64,
            PixelHeight = 64,
            Dpi = 300
        };

        // En Windows, 'copy {input} {output}' simula un procesador externo que genera la imagen
        string copyCommand = "copy /y {input} {output}";
        var processed = await service.ProcessPageAsync(page, copyCommand);

        Assert.NotNull(processed.OriginalImageBytes);
        Assert.Equal(rawBytes.Length, processed.OriginalImageBytes!.Length);
        Assert.NotEmpty(processed.ImageBytes);
    }

    [Fact]
    public async Task ProcessPageAsync_WhenCommandFails_ThrowsAndKeepsOriginal()
    {
        var service = new CommandPostprocessingService();
        var rawBytes = CreateSamplePngBytes(64, 64);
        var page = new ScannedPage
        {
            ImageBytes = rawBytes,
            PixelWidth = 64,
            PixelHeight = 64,
            Dpi = 300
        };

        string failingCommand = "cmd /c exit 2";
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessPageAsync(page, failingCommand));

        // Comprueba que los bytes originales fueron resguardados y no se perdieron
        Assert.NotNull(page.OriginalImageBytes);
        Assert.Equal(rawBytes, page.ImageBytes);
    }

    [Fact]
    public async Task RevertToOriginalAsync_RestoresOriginalBytes()
    {
        var service = new CommandPostprocessingService();
        var originalBytes = CreateSamplePngBytes(80, 80);
        var modifiedBytes = CreateSamplePngBytes(100, 100);

        var page = new ScannedPage
        {
            ImageBytes = modifiedBytes,
            OriginalImageBytes = originalBytes,
            PixelWidth = 100,
            PixelHeight = 100
        };

        bool reverted = await service.RevertToOriginalAsync(page);
        Assert.True(reverted);
        Assert.Equal(80, page.PixelWidth);
        Assert.Equal(80, page.PixelHeight);
        Assert.Equal(originalBytes, page.ImageBytes);
    }

    [Fact]
    public async Task MainViewModel_AddAcquiredPage_WhenPostprocessingFails_PreservesPageWithoutCrashing()
    {
        var fakeScanner = new MockScannerService();
        var fakePdf = new PdfSharpExportService();
        var fakeImage = new ImageSharpExportService();
        var fakePrint = new SystemDrawingPrintService();
        var fakeSystem = new FakeSystemService();
        var autosaveService = new FakeAutosaveService();
        var failingPostprocessing = new FailingPostprocessingService();
        var fakeDialog = new FakeDialogService();
        var fakeSettings = new FakeSettingsService
        {
            CurrentSettings = new AppSettings
            {
                IsPostprocessingEnabled = true,
                PostprocessingCommand = "failing_command {input} {output}"
            }
        };

        var vm = new MainViewModel(
            fakeScanner, fakeScanner, fakePdf, fakeImage, fakePrint, fakeSystem,
            autosaveService, failingPostprocessing, fakeDialog, fakeSettings);

        var page = new ScannedPage
        {
            ImageBytes = CreateSamplePngBytes(64, 64),
            PixelWidth = 64,
            PixelHeight = 64
        };

        // No debe lanzar excepción; debe conservar la página
        await vm.AddAcquiredPageAsync(page);

        Assert.Single(vm.Pages);
        Assert.Equal(page, vm.Pages[0].Model);
    }

    private class FailingPostprocessingService : IPostprocessingService
    {
        public Task<ScannedPage> ProcessPageAsync(ScannedPage page, string commandTemplate, System.Threading.CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("External script not found");
        }

        public Task<bool> RevertToOriginalAsync(ScannedPage page) => Task.FromResult(true);
    }

    private class FakeAutosaveService : IAutosaveService
    {
        public Task ClearSessionAsync() => Task.CompletedTask;
        public Task<bool> HasAutosavedSessionAsync() => Task.FromResult(false);
        public Task<IReadOnlyList<ScannedPage>> RecoverSessionAsync() => Task.FromResult<IReadOnlyList<ScannedPage>>(Array.Empty<ScannedPage>());
        public Task SaveSessionAsync(IReadOnlyList<ScannedPageItemViewModel> pages) => Task.CompletedTask;
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
