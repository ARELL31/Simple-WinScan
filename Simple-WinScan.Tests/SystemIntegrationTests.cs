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

public class SystemIntegrationTests
{
    private static byte[] CreateSamplePngBytes(int width, int height)
    {
        using var img = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    private class FakeSystemService : ISystemIntegrationService
    {
        public bool OpenPageInvoked { get; private set; }
        public bool OpenDocInvoked { get; private set; }
        public bool EmailInvoked { get; private set; }
        public bool ShareInvoked { get; private set; }
        public ScannedPage? LastPage { get; private set; }
        public IReadOnlyList<ScannedPage>? LastPages { get; private set; }

        public Task<bool> OpenInExternalViewerAsync(ScannedPage page)
        {
            OpenPageInvoked = true;
            LastPage = page;
            return Task.FromResult(true);
        }

        public Task<bool> OpenDocumentInExternalViewerAsync(IReadOnlyList<ScannedPage> pages)
        {
            OpenDocInvoked = true;
            LastPages = pages;
            return Task.FromResult(true);
        }

        public Task<bool> EmailPagesAsync(IReadOnlyList<ScannedPage> pages, string subject = "Documento Escaneado")
        {
            EmailInvoked = true;
            LastPages = pages;
            return Task.FromResult(true);
        }

        public Task<bool> SharePagesAsync(IReadOnlyList<ScannedPage> pages, string title = "Documento Escaneado")
        {
            ShareInvoked = true;
            LastPages = pages;
            return Task.FromResult(true);
        }
    }

    [Fact]
    public async Task MainViewModel_SystemIntegrationCommands_InvokeCorrectMethods()
    {
        var fakeScanner = new MockScannerService();
        var fakePdf = new PdfSharpExportService();
        var fakeImage = new ImageSharpExportService();
        var fakePrint = new SystemDrawingPrintService();
        var fakeSystem = new FakeSystemService();
        var fakeDialog = new FakeDialogService();
        var fakeSettings = new FakeSettingsService();

        var vm = new MainViewModel(fakeScanner, fakeScanner, fakePdf, fakeImage, fakePrint, fakeSystem, fakeDialog, fakeSettings);

        var page = new ScannedPage
        {
            ImageBytes = CreateSamplePngBytes(100, 100),
            PixelWidth = 100,
            PixelHeight = 100
        };
        await vm.AddAcquiredPageAsync(page);

        await vm.OpenSelectedInExternalViewerAsync();
        Assert.True(fakeSystem.OpenPageInvoked);
        Assert.Same(page, fakeSystem.LastPage);

        await vm.OpenDocumentInExternalViewerAsync();
        Assert.True(fakeSystem.OpenDocInvoked);
        Assert.Single(fakeSystem.LastPages!);

        await vm.EmailDocumentAsync();
        Assert.True(fakeSystem.EmailInvoked);

        await vm.ShareDocumentAsync();
        Assert.True(fakeSystem.ShareInvoked);
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
