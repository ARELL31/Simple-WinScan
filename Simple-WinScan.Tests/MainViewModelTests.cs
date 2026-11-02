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
        public Func<ScannerDeviceInfo?, ScanProfile, IProgress<int>?, CancellationToken, Task<ScannedPage>>? OnScanPage { get; set; }

        public Task<IReadOnlyList<ScannerDeviceInfo>> GetAvailableScannersAsync()
        {
            if (OnGetAvailableScanners != null) return OnGetAvailableScanners();
            return Task.FromResult<IReadOnlyList<ScannerDeviceInfo>>(new List<ScannerDeviceInfo>());
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
            if (OnScanPage != null) return OnScanPage(scanner, profile, progress, cancellationToken);
            return Task.FromResult(new ScannedPage { ImageBytes = new byte[] { 1, 2, 3 }, PixelWidth = 100, PixelHeight = 100, Dpi = 300 });
        }

        public async Task<IReadOnlyList<ScannedPage>> ScanBatchAsync(
            ScannerDeviceInfo? scanner,
            ScanProfile profile,
            Action<ScannedPage> onPageAcquired,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var page = await ScanPageAsync(scanner, profile, progress, cancellationToken);
            onPageAcquired?.Invoke(page);
            return new List<ScannedPage> { page };
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
    public async Task ScanAsync_WhenScannerHardwareExceptionThrown_PreservesPagesCollectionAndEnablesRetry()
    {
        var fakeScanner = new FakeScannerService
        {
            OnScanPage = (scanner, profile, prog, ct) =>
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
    public async Task ScanAsync_WhenOperationCanceled_PreservesPagesAndDisplaysCancelNotification()
    {
        var fakeScanner = new FakeScannerService
        {
            OnScanPage = (scanner, profile, prog, ct) =>
                throw new OperationCanceledException("Operación cancelada por el usuario.")
        };

        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService());
        var physicalDevice = new ScannerDeviceInfo { Id = "Dev001", Name = "Canon LiDE 300" };
        vm.AvailableScanners.Add(physicalDevice);
        vm.SelectedScanner = physicalDevice;

        // Act
        await vm.ScanAsync();

        // Assert
        Assert.Empty(vm.Pages);
        Assert.False(vm.IsScanning);
        Assert.True(vm.CanScan);
        Assert.True(vm.IsInfoBarOpen);
        Assert.Equal(InfoBarSeverity.Informational, vm.InfoBarSeverity);
        Assert.Equal("Escaneo Cancelado", vm.InfoBarTitle);
        Assert.Contains("cancelado", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelScanCommand_WhenScanning_SetsCancelStatusAndTriggersCancellation()
    {
        var tcs = new TaskCompletionSource<ScannedPage>();
        var fakeScanner = new FakeScannerService
        {
            OnScanPage = async (scanner, profile, prog, ct) =>
            {
                using (ct.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task;
                }
            }
        };

        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService());
        var physicalDevice = new ScannerDeviceInfo { Id = "Dev001", Name = "Canon LiDE 300" };
        vm.AvailableScanners.Add(physicalDevice);
        vm.SelectedScanner = physicalDevice;

        // Iniciar escaneo en segundo plano
        var scanTask = vm.ScanAsync();

        Assert.True(vm.IsScanning);

        // Act: Cancelar escaneo
        vm.CancelScan();

        // Esperar resolución
        await scanTask;

        Assert.Equal("Escaneo cancelado por el usuario.", vm.StatusMessage);
        Assert.False(vm.IsScanning);
        Assert.True(vm.CanScan);
    }

    [Fact]
    public async Task ScanAsync_UpdatesProgressPercentageAndIndeterminateState()
    {
        var progressValues = new List<int>();
        var fakeScanner = new FakeScannerService
        {
            OnScanPage = async (scanner, profile, prog, ct) =>
            {
                prog?.Report(35);
                await Task.Delay(25);
                prog?.Report(70);
                await Task.Delay(25);
                prog?.Report(100);
                await Task.Delay(25);
                return new ScannedPage { ImageBytes = new byte[] { 1, 2, 3 }, PixelWidth = 100, PixelHeight = 100 };
            }
        };

        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService());
        var physicalDevice = new ScannerDeviceInfo { Id = "Dev001", Name = "Canon LiDE 300" };
        vm.AvailableScanners.Add(physicalDevice);
        vm.SelectedScanner = physicalDevice;

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ScanProgressPercentage))
            {
                progressValues.Add(vm.ScanProgressPercentage);
            }
        };

        await vm.ScanAsync();

        Assert.Contains(35, progressValues);
        Assert.Contains(70, progressValues);
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

    [Fact]
    public async Task InitializeAsync_WhenSavedScannerExists_PreselectsSavedScanner()
    {
        var scanner1 = new ScannerDeviceInfo { Id = "DEV_A", Name = "Scanner A" };
        var scanner2 = new ScannerDeviceInfo { Id = "DEV_B", Name = "Scanner B" };
        var fakeScanner = new FakeScannerService
        {
            OnGetAvailableScanners = () => Task.FromResult<IReadOnlyList<ScannerDeviceInfo>>(new List<ScannerDeviceInfo> { scanner1, scanner2 })
        };
        var fakeSettings = new FakeSettingsService
        {
            CurrentSettings = new AppSettings { LastSelectedScannerId = "DEV_B" }
        };

        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService(), fakeSettings);
        await vm.InitializeAsync();

        Assert.NotNull(vm.SelectedScanner);
        Assert.Equal("DEV_B", vm.SelectedScanner.Id);
    }

    [Fact]
    public async Task SelectedScanner_WhenChanged_SavesToSettings()
    {
        var scanner1 = new ScannerDeviceInfo { Id = "DEV_A", Name = "Scanner A" };
        var scanner2 = new ScannerDeviceInfo { Id = "DEV_B", Name = "Scanner B" };
        var fakeScanner = new FakeScannerService
        {
            OnGetAvailableScanners = () => Task.FromResult<IReadOnlyList<ScannerDeviceInfo>>(new List<ScannerDeviceInfo> { scanner1, scanner2 })
        };
        var fakeSettings = new FakeSettingsService();

        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService(), fakeSettings);
        await vm.InitializeAsync();

        vm.SelectedScanner = scanner2;

        Assert.Equal("DEV_B", fakeSettings.CurrentSettings.LastSelectedScannerId);
    }

    [Fact]
    public async Task LoadScannersAsync_HotRefresh_PreservesCurrentSelectionIfStillPresent()
    {
        var scanner1 = new ScannerDeviceInfo { Id = "DEV_A", Name = "Scanner A" };
        var scanner2 = new ScannerDeviceInfo { Id = "DEV_B", Name = "Scanner B" };
        var currentList = new List<ScannerDeviceInfo> { scanner1, scanner2 };

        var fakeScanner = new FakeScannerService
        {
            OnGetAvailableScanners = () => Task.FromResult<IReadOnlyList<ScannerDeviceInfo>>(currentList)
        };
        var fakeSettings = new FakeSettingsService();

        var vm = new MainViewModel(fakeScanner, new MockScannerService(), new FakePdfExportService(), new FakeDialogService(), fakeSettings);
        await vm.InitializeAsync();
        vm.SelectedScanner = scanner2;

        // Simular que se conecta un tercer escáner por USB (hot-plug)
        var scanner3 = new ScannerDeviceInfo { Id = "DEV_C", Name = "Scanner C" };
        currentList.Add(scanner3);

        await vm.LoadScannersAsync();

        Assert.Equal(3, vm.AvailableScanners.Count);
        Assert.NotNull(vm.SelectedScanner);
        Assert.Equal("DEV_B", vm.SelectedScanner.Id);
    }
}
