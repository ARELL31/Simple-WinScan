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

public class MainViewModelTests
{
    private class FakeScannerService : IScannerService
    {
        public bool IsMockMode => false;
        public Func<Task<IReadOnlyList<ScannerDeviceInfo>>>? OnGetAvailableScanners { get; set; }
        public Func<ScannerDeviceInfo?, ScanProfile, CancellationToken, Task<ScannedPage>>? OnScanPage { get; set; }

        public Task<IReadOnlyList<ScannerDeviceInfo>> GetAvailableScannersAsync()
        {
            if (OnGetAvailableScanners != null) return OnGetAvailableScanners();
            return Task.FromResult<IReadOnlyList<ScannerDeviceInfo>>(new List<ScannerDeviceInfo>());
        }

        public Task<ScannedPage> ScanPageAsync(ScannerDeviceInfo? scanner, ScanProfile profile, CancellationToken cancellationToken = default)
        {
            if (OnScanPage != null) return OnScanPage(scanner, profile, cancellationToken);
            return Task.FromResult(new ScannedPage { ImageBytes = new byte[] { 1, 2, 3 }, PixelWidth = 100, PixelHeight = 100, Dpi = 300 });
        }
    }

    private class FakePdfExportService : IPdfExportService
    {
        public Task ExportPagesToPdfAsync(IReadOnlyList<ScannedPage> pages, Stream outputStream, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class FakeDialogService : IDialogService
    {
        public Task<string?> PickSavePdfPathAsync(string defaultFileName) => Task.FromResult<string?>("C:\\fake.pdf");
        public Task ShowMessageAsync(string title, string message) => Task.CompletedTask;
        public Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText) => Task.FromResult(true);
    }

    [Fact]
    public async Task ScanAsync_WhenScannerHardwareExceptionThrown_PreservesPagesCollectionAndEnablesRetry()
    {
        var fakeScanner = new FakeScannerService
        {
            OnScanPage = (scanner, profile, ct) =>
                throw new ScannerHardwareException("Fallo en hardware: lámpara bloqueada o atasco de papel.", unchecked((int)0x80210002))
        };

        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService());
        
        // Simular página previa existente en el documento
        var initialPage = new ScannedPage { ImageBytes = new byte[] { 0xAA, 0xBB }, PixelWidth = 200, PixelHeight = 200, Dpi = 300 };
        var initialItemVm = new ScannedPageItemViewModel(initialPage, 1);
        vm.Pages.Add(initialItemVm);

        var physicalDevice = new ScannerDeviceInfo
        {
            Id = "Dev001",
            Name = "Canon LiDE 300",
            Manufacturer = "Canon",
            IsDefault = true
        };
        vm.AvailableScanners.Add(physicalDevice);
        vm.SelectedScanner = physicalDevice;

        Assert.Single(vm.Pages);
        Assert.True(vm.CanScan);

        // Act
        await vm.ScanAsync();

        // Assert: La colección de páginas NO debe modificarse ante un error físico
        Assert.Single(vm.Pages);
        Assert.Same(initialItemVm, vm.Pages[0]);

        // La aplicación debe reportar el error en InfoBar
        Assert.True(vm.IsInfoBarOpen);
        Assert.Equal(InfoBarSeverity.Error, vm.InfoBarSeverity);
        Assert.Equal("Fallo de Hardware del Escáner", vm.InfoBarTitle);
        Assert.Contains("atasco de papel", vm.InfoBarMessage);

        // La aplicación debe permitir el reintento inmediato
        Assert.False(vm.IsScanning);
        Assert.True(vm.CanScan);
        Assert.Contains("reintentar", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ToggleVirtualScannerAsync_AddsAndRemovesMockDeviceExplicitly()
    {
        var fakeScanner = new FakeScannerService
        {
            OnGetAvailableScanners = () => Task.FromResult<IReadOnlyList<ScannerDeviceInfo>>(new List<ScannerDeviceInfo>())
        };

        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService());

        // Inicialmente sin escáner virtual
        await vm.InitializeAsync();
        Assert.False(vm.IsVirtualScannerEnabled);
        Assert.Empty(vm.AvailableScanners);

        // Act 1: Activar simulación explícita
        await vm.ToggleVirtualScannerAsync();
        Assert.True(vm.IsVirtualScannerEnabled);
        Assert.Single(vm.AvailableScanners);
        Assert.Equal(MockScannerService.MockScannerId, vm.AvailableScanners[0].Id);
        Assert.Equal(MockScannerService.MockScannerId, vm.SelectedScanner?.Id);

        // Act 2: Desactivar simulación explícita
        await vm.ToggleVirtualScannerAsync();
        Assert.False(vm.IsVirtualScannerEnabled);
        Assert.Empty(vm.AvailableScanners);
    }

    [Fact]
    public async Task ScanAsync_WhenNoScannerSelected_ShowsWarningWithoutScanning()
    {
        var fakeScanner = new FakeScannerService();
        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService());
        vm.SelectedScanner = null;

        await vm.ScanAsync();

        Assert.False(vm.IsScanning);
        Assert.True(vm.IsInfoBarOpen);
        Assert.Equal(InfoBarSeverity.Warning, vm.InfoBarSeverity);
        Assert.Equal("Sin escáner seleccionado", vm.InfoBarTitle);
        Assert.Empty(vm.Pages);
    }
}
