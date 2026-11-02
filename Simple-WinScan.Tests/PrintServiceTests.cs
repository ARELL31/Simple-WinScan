using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Simple_WinScan.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Simple_WinScan.Services.Implementations;
using Simple_WinScan.Services.Interfaces;
using Simple_WinScan.ViewModels;
using Xunit;

namespace Simple_WinScan.Tests;

public class PrintServiceTests
{
    private static byte[] CreateSamplePngBytes(int width, int height)
    {
        using var img = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    private class FakePrintService : IPrintService
    {
        public bool PrintInvoked { get; private set; }
        public IReadOnlyList<ScannedPage>? LastPages { get; private set; }
        public PrintOptions? LastOptions { get; private set; }

        public IReadOnlyList<string> GetInstalledPrinters() => new[] { "Virtual Test Printer", "Microsoft Print to PDF" };
        public string? GetDefaultPrinter() => "Virtual Test Printer";

        public Task<bool> PrintPagesAsync(IReadOnlyList<ScannedPage> pages, PrintOptions? options = null, CancellationToken cancellationToken = default)
        {
            PrintInvoked = true;
            LastPages = pages;
            LastOptions = options;
            return Task.FromResult(true);
        }
    }

    [Fact]
    public void GetInstalledPrinters_ReturnsValidCollection()
    {
        var service = new SystemDrawingPrintService();
        var printers = service.GetInstalledPrinters();
        Assert.NotNull(printers);
    }

    [Fact]
    public async Task PrintPagesAsync_EmptyList_ReturnsFalse()
    {
        var service = new SystemDrawingPrintService();
        bool result = await service.PrintPagesAsync(Array.Empty<ScannedPage>());
        Assert.False(result);
    }

    [Fact]
    public async Task MainViewModel_PrintDocumentAsync_InvokesPrintService()
    {
        var fakeScanner = new MockScannerService();
        var fakePdf = new PdfSharpExportService();
        var fakeImage = new ImageSharpExportService();
        var fakePrint = new FakePrintService();
        var fakeDialog = new FakeDialogService();
        var fakeSettings = new FakeSettingsService();

        var vm = new MainViewModel(fakeScanner, fakeScanner, fakePdf, fakeImage, fakePrint, fakeDialog, fakeSettings);

        var page = new ScannedPage
        {
            ImageBytes = CreateSamplePngBytes(100, 100),
            PixelWidth = 100,
            PixelHeight = 100
        };
        await vm.AddAcquiredPageAsync(page);

        // Sin XamlRoot activo (ejecución de prueba), invoca fallback directo a PrintPagesAsync
        await vm.PrintDocumentAsync();

        Assert.True(fakePrint.PrintInvoked);
        Assert.NotNull(fakePrint.LastPages);
        Assert.Single(fakePrint.LastPages!);
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
