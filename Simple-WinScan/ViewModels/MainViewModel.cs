using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Simple_WinScan.Helpers;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Exceptions;
using Simple_WinScan.Services.Implementations;
using Simple_WinScan.Services.Interfaces;

namespace Simple_WinScan.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IScannerService _scannerService;
    private readonly MockScannerService _mockScannerService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IImageExportService _imageExportService;
    private readonly IPrintService _printService;
    private readonly ISystemIntegrationService _systemService;
    private readonly IAutosaveService _autosaveService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _scanCts;

    public ObservableCollection<ScannedPageItemViewModel> Pages { get; } = new();
    public ObservableCollection<ScannerDeviceInfo> AvailableScanners { get; } = new();
    public ObservableCollection<ScanProfile> AvailableProfiles { get; } = new();

    [ObservableProperty]
    private ScannerDeviceInfo? _selectedScanner;

    [ObservableProperty]
    private ScanProfile _selectedProfile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPage))]
    private ScannedPageItemViewModel? _selectedPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScan))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool _isScanning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScan))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool _isExporting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScan))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool _isContinuousScanActive;

    [ObservableProperty]
    private int _continuousCountdownSeconds;

    [ObservableProperty]
    private int _continuousScanDelaySeconds = 5;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _hasRecoverableSession;

    [ObservableProperty]
    private string _recoveryMessage = string.Empty;

    [ObservableProperty]
    private bool _isVirtualScannerEnabled;

    [ObservableProperty]
    private int _scanProgressPercentage;

    [ObservableProperty]
    private bool _isProgressIndeterminate = true;

    [ObservableProperty]
    private string _statusMessage = "Listo para escanear";

    [ObservableProperty]
    private string _pageCountSummary = "0 páginas";

    [ObservableProperty]
    private bool _hasPages;

    [ObservableProperty]
    private bool _isInfoBarOpen;

    [ObservableProperty]
    private string _infoBarTitle = string.Empty;

    [ObservableProperty]
    private string _infoBarMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _infoBarSeverity = InfoBarSeverity.Informational;

    public bool HasSelectedPage => SelectedPage != null;
    public bool CanScan => !IsScanning && !IsExporting && !IsContinuousScanActive;
    public bool CanSave => HasPages && !IsScanning && !IsExporting && !IsContinuousScanActive;

    public MainViewModel(
        IScannerService scannerService,
        IPdfExportService pdfExportService,
        IDialogService dialogService)
        : this(scannerService, new MockScannerService(), pdfExportService, new ImageSharpExportService(), new SystemDrawingPrintService(), new WindowsSystemIntegrationService(pdfExportService), dialogService, new JsonSettingsService())
    {
    }

    public MainViewModel(
        IScannerService scannerService,
        MockScannerService mockScannerService,
        IPdfExportService pdfExportService,
        IDialogService dialogService)
        : this(scannerService, mockScannerService, pdfExportService, new ImageSharpExportService(), new SystemDrawingPrintService(), new WindowsSystemIntegrationService(pdfExportService), dialogService, new JsonSettingsService())
    {
    }

    public MainViewModel(
        IScannerService scannerService,
        MockScannerService mockScannerService,
        IPdfExportService pdfExportService,
        IDialogService dialogService,
        ISettingsService settingsService)
        : this(scannerService, mockScannerService, pdfExportService, new ImageSharpExportService(), new SystemDrawingPrintService(), new WindowsSystemIntegrationService(pdfExportService), dialogService, settingsService)
    {
    }

    public MainViewModel(
        IScannerService scannerService,
        MockScannerService mockScannerService,
        IPdfExportService pdfExportService,
        IImageExportService imageExportService,
        IDialogService dialogService,
        ISettingsService settingsService)
        : this(scannerService, mockScannerService, pdfExportService, imageExportService, new SystemDrawingPrintService(), new WindowsSystemIntegrationService(pdfExportService), dialogService, settingsService)
    {
    }

    public MainViewModel(
        IScannerService scannerService,
        MockScannerService mockScannerService,
        IPdfExportService pdfExportService,
        IImageExportService imageExportService,
        IPrintService printService,
        IDialogService dialogService,
        ISettingsService settingsService)
        : this(scannerService, mockScannerService, pdfExportService, imageExportService, printService, new WindowsSystemIntegrationService(pdfExportService), dialogService, settingsService)
    {
    }

    public MainViewModel(
        IScannerService scannerService,
        MockScannerService mockScannerService,
        IPdfExportService pdfExportService,
        IImageExportService imageExportService,
        IPrintService printService,
        ISystemIntegrationService systemService,
        IDialogService dialogService,
        ISettingsService settingsService)
        : this(scannerService, mockScannerService, pdfExportService, imageExportService, printService, systemService, new JsonAutosaveService(), dialogService, settingsService)
    {
    }

    public MainViewModel(
        IScannerService scannerService,
        MockScannerService mockScannerService,
        IPdfExportService pdfExportService,
        IImageExportService imageExportService,
        IPrintService printService,
        ISystemIntegrationService systemService,
        IAutosaveService autosaveService,
        IDialogService dialogService,
        ISettingsService settingsService)
    {
        _scannerService = scannerService ?? throw new ArgumentNullException(nameof(scannerService));
        _mockScannerService = mockScannerService ?? throw new ArgumentNullException(nameof(mockScannerService));
        _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
        _imageExportService = imageExportService ?? throw new ArgumentNullException(nameof(imageExportService));
        _printService = printService ?? throw new ArgumentNullException(nameof(printService));
        _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
        _autosaveService = autosaveService ?? throw new ArgumentNullException(nameof(autosaveService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Inicializar perfiles estándar
        AvailableProfiles.Add(ScanProfile.TextDefault);
        AvailableProfiles.Add(ScanProfile.PhotoDefault);
        _selectedProfile = AvailableProfiles[0];
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        IsVirtualScannerEnabled = settings.IsVirtualScannerEnabled;
        await LoadScannersAsync(savedScannerId: settings.LastSelectedScannerId);

        try
        {
            bool hasAutosave = await _autosaveService.HasAutosavedSessionAsync();
            if (hasAutosave)
            {
                HasRecoverableSession = true;
                RecoveryMessage = "Se detectó un documento no guardado de una sesión anterior inesperada.";
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task LoadScannersAsync()
    {
        await LoadScannersAsync(savedScannerId: null);
    }

    public async Task LoadScannersAsync(string? savedScannerId)
    {
        var previousSelectionId = SelectedScanner?.Id ?? savedScannerId;
        AvailableScanners.Clear();
        var physicalScanners = await _scannerService.GetAvailableScannersAsync();
        foreach (var scanner in physicalScanners)
        {
            AvailableScanners.Add(scanner);
        }

        if (IsVirtualScannerEnabled)
        {
            AvailableScanners.Add(MockScannerService.CreateMockDeviceInfo());
        }

        if (!string.IsNullOrEmpty(previousSelectionId))
        {
            SelectedScanner = AvailableScanners.FirstOrDefault(s => s.Id == previousSelectionId);
        }

        if (SelectedScanner == null || !AvailableScanners.Contains(SelectedScanner))
        {
            SelectedScanner = AvailableScanners.FirstOrDefault(s => s.IsDefault) ?? AvailableScanners.FirstOrDefault();
        }

        if (AvailableScanners.Count == 0)
        {
            ShowNotification(
                "Sin escáner físico detectado",
                "No se detectó ningún escáner WIA conectado. Conecte un escáner USB o active el escáner virtual desde el menú de opciones [...].",
                InfoBarSeverity.Informational);
        }
    }

    partial void OnSelectedScannerChanged(ScannerDeviceInfo? value)
    {
        if (value != null)
        {
            _ = SaveSelectedScannerSettingAsync(value.Id);
        }
    }

    private async Task SaveSelectedScannerSettingAsync(string scannerId)
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync();
            settings.LastSelectedScannerId = scannerId;
            await _settingsService.SaveSettingsAsync(settings);
        }
        catch { }
    }

    [RelayCommand]
    public async Task ToggleVirtualScannerAsync()
    {
        IsVirtualScannerEnabled = !IsVirtualScannerEnabled;
        try
        {
            var settings = await _settingsService.LoadSettingsAsync();
            settings.IsVirtualScannerEnabled = IsVirtualScannerEnabled;
            await _settingsService.SaveSettingsAsync(settings);
        }
        catch { }

        await LoadScannersAsync();

        if (IsVirtualScannerEnabled)
        {
            SelectedScanner = AvailableScanners.FirstOrDefault(s => s.Id == MockScannerService.MockScannerId);
            ShowNotification("Modo Simulación Habilitado", "Se ha activado el escáner virtual para pruebas sin hardware.", InfoBarSeverity.Informational);
        }
        else
        {
            ShowNotification("Modo Simulación Deshabilitado", "Se restauró la lista de escáneres exclusivamente físicos.", InfoBarSeverity.Informational);
        }
    }

    [RelayCommand]
    public async Task ScanAsync()
    {
        if (IsScanning) return;

        if (SelectedScanner == null)
        {
            ShowNotification(
                "Sin escáner seleccionado",
                "Por favor seleccione un escáner físico o active el escáner virtual de prueba desde el menú [...].",
                InfoBarSeverity.Warning);
            return;
        }

        IsScanning = true;
        ScanProgressPercentage = 0;
        IsProgressIndeterminate = true;
        StatusMessage = $"Escaneando página con '{SelectedScanner.Name}'...";
        _scanCts = new CancellationTokenSource();

        try
        {
            var activeService = (SelectedScanner.Id == MockScannerService.MockScannerId)
                ? (IScannerService)_mockScannerService
                : _scannerService;

            var progress = new Progress<int>(percent =>
            {
                IsProgressIndeterminate = false;
                ScanProgressPercentage = percent;
                StatusMessage = $"Escaneando con '{SelectedScanner.Name}' ({percent}%)...";
            });

            var scannedPage = await activeService.ScanPageAsync(SelectedScanner, SelectedProfile, progress, _scanCts.Token);
            await AddAcquiredPageAsync(scannedPage);

            StatusMessage = $"Página {Pages.Count} digitalizada con éxito.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Escaneo cancelado por el usuario.";
            ShowNotification("Escaneo Cancelado", "La digitalización fue detenida a petición del usuario.", InfoBarSeverity.Informational);
        }
        catch (ScannerHardwareException ex)
        {
            StatusMessage = "Fallo del escáner. Puede reintentar la operación.";
            ShowNotification("Fallo de Hardware del Escáner", ex.Message, InfoBarSeverity.Error);
        }
        catch (Exception ex)
        {
            StatusMessage = "Error inesperado durante el escaneo.";
            ShowNotification("Error de Escaneo", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsScanning = false;
            IsProgressIndeterminate = true;
            ScanProgressPercentage = 0;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    [RelayCommand]
    public async Task ScanBatchAdfAsync()
    {
        if (IsScanning || IsContinuousScanActive) return;

        if (SelectedScanner == null)
        {
            ShowNotification(
                "Sin escáner seleccionado",
                "Por favor seleccione un escáner físico o active el escáner virtual de prueba desde el menú [...].",
                InfoBarSeverity.Warning);
            return;
        }

        IsScanning = true;
        ScanProgressPercentage = 0;
        IsProgressIndeterminate = true;
        StatusMessage = $"Iniciando escaneo por lotes ADF con '{SelectedScanner.Name}'...";
        _scanCts = new CancellationTokenSource();
        int initialPageCount = Pages.Count;

        try
        {
            var activeService = (SelectedScanner.Id == MockScannerService.MockScannerId)
                ? (IScannerService)_mockScannerService
                : _scannerService;

            var adfProfile = SelectedProfile with { Source = ScanSource.Feeder };

            var progress = new Progress<int>(percent =>
            {
                IsProgressIndeterminate = false;
                ScanProgressPercentage = percent;
                StatusMessage = $"Escaneando lote con '{SelectedScanner.Name}' ({percent}%)...";
            });

            await activeService.ScanBatchAsync(
                SelectedScanner,
                adfProfile,
                onPageAcquired: async page =>
                {
                    await AddAcquiredPageAsync(page);
                    StatusMessage = $"Página {Pages.Count} digitalizada del alimentador.";
                },
                progress,
                _scanCts.Token);

            int batchAcquired = Pages.Count - initialPageCount;
            StatusMessage = $"Lote completado: {batchAcquired} páginas digitalizadas desde alimentador.";
            ShowNotification("Lote ADF Finalizado", $"Se digitalizaron {batchAcquired} páginas correctamente.", InfoBarSeverity.Success);
        }
        catch (OperationCanceledException)
        {
            int batchAcquired = Pages.Count - initialPageCount;
            StatusMessage = $"Escaneo de lote cancelado por el usuario ({batchAcquired} páginas conservadas).";
            ShowNotification("Lote Cancelado", $"La digitalización se detuvo. Se conservaron {batchAcquired} páginas.", InfoBarSeverity.Informational);
        }
        catch (ScannerHardwareException ex)
        {
            int batchAcquired = Pages.Count - initialPageCount;
            StatusMessage = $"Interrupción en el alimentador. Se conservaron {batchAcquired} páginas.";
            ShowNotification(
                "Fallo en Alimentador Automático",
                $"{ex.Message} Se conservaron {batchAcquired} páginas en el documento. Despeje el alimentador para continuar.",
                InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            int batchAcquired = Pages.Count - initialPageCount;
            StatusMessage = $"Error durante escaneo de lote ({batchAcquired} páginas conservadas).";
            ShowNotification("Error de Lote ADF", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsScanning = false;
            IsProgressIndeterminate = true;
            ScanProgressPercentage = 0;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    [RelayCommand]
    public async Task StartContinuousScanAsync()
    {
        if (IsScanning || IsContinuousScanActive) return;

        if (SelectedScanner == null)
        {
            ShowNotification(
                "Sin escáner seleccionado",
                "Por favor seleccione un escáner físico o active el escáner virtual de prueba.",
                InfoBarSeverity.Warning);
            return;
        }

        IsContinuousScanActive = true;
        _scanCts = new CancellationTokenSource();

        try
        {
            var activeService = (SelectedScanner.Id == MockScannerService.MockScannerId)
                ? (IScannerService)_mockScannerService
                : _scannerService;

            while (IsContinuousScanActive && !_scanCts.IsCancellationRequested)
            {
                IsScanning = true;
                IsProgressIndeterminate = true;
                ScanProgressPercentage = 0;
                StatusMessage = $"Escaneando página continua con '{SelectedScanner.Name}'...";

                var progress = new Progress<int>(percent =>
                {
                    IsProgressIndeterminate = false;
                    ScanProgressPercentage = percent;
                });

                var page = await activeService.ScanPageAsync(SelectedScanner, SelectedProfile, progress, _scanCts.Token);
                await AddAcquiredPageAsync(page);

                IsScanning = false;
                StatusMessage = $"Página {Pages.Count} digitalizada.";

                int delay = ContinuousScanDelaySeconds > 0 ? ContinuousScanDelaySeconds : 5;
                for (int s = delay; s > 0; s--)
                {
                    if (!IsContinuousScanActive || _scanCts.IsCancellationRequested) break;
                    ContinuousCountdownSeconds = s;
                    StatusMessage = $"Página {Pages.Count} lista. Próxima captura en {s} s... [Detener para terminar]";
                    await Task.Delay(1000, _scanCts.Token);
                }

                if (!IsContinuousScanActive || _scanCts.IsCancellationRequested) break;

                PlayBeepNotification();
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Modo continuo detenido.";
        }
        catch (ScannerHardwareException ex)
        {
            StatusMessage = "Fallo del escáner en modo continuo.";
            ShowNotification("Fallo de Hardware", ex.Message, InfoBarSeverity.Error);
        }
        catch (Exception ex)
        {
            StatusMessage = "Error en escaneo continuo.";
            ShowNotification("Error en Modo Continuo", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsContinuousScanActive = false;
            IsScanning = false;
            IsProgressIndeterminate = true;
            ScanProgressPercentage = 0;
            ContinuousCountdownSeconds = 0;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    [RelayCommand]
    public void StopContinuousScan()
    {
        IsContinuousScanActive = false;
        _scanCts?.Cancel();
    }

    public async Task AddAcquiredPageAsync(ScannedPage scannedPage)
    {
        int nextNumber = Pages.Count + 1;
        var itemVm = new ScannedPageItemViewModel(
            scannedPage,
            nextNumber,
            onDeleteRequested: OnPageDeleteRequested,
            onPageUpdated: OnPageModified);

        await itemVm.InitializeThumbnailAsync();

        Pages.Add(itemVm);
        SelectedPage = itemVm;
        UpdatePageSummary();
        IsDirty = true;
        TriggerAutosave();
    }

    private void OnPageModified()
    {
        IsDirty = true;
        UpdatePageSummary();
        TriggerAutosave();
    }

    [RelayCommand]
    public void CancelScan()
    {
        if (IsContinuousScanActive)
        {
            StopContinuousScan();
            return;
        }

        if (IsScanning && _scanCts != null && !_scanCts.IsCancellationRequested)
        {
            StatusMessage = "Cancelando escaneo...";
            _scanCts.Cancel();
        }
    }

    [RelayCommand]
    public async Task SavePdfAsync()
    {
        if (!CanSave || Pages.Count == 0) return;

        string defaultFileName = $"Documento_{DateTime.Now:yyyy-MM-dd_HHmm}.pdf";
        string? targetPath = await _dialogService.PickSavePdfPathAsync(defaultFileName);

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return; // Cancelado por el usuario
        }

        IsExporting = true;
        StatusMessage = "Generando archivo PDF...";

        try
        {
            var rawPages = Pages.Select(p => p.Model).ToList();
            using var fileStream = File.Create(targetPath);
            await _pdfExportService.ExportPagesToPdfAsync(rawPages, fileStream);

            IsDirty = false;
            _ = _autosaveService.ClearSessionAsync();
            StatusMessage = "PDF guardado exitosamente.";
            ShowNotification("Documento Guardado", $"Se ha exportado correctamente a:\n{targetPath}", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = "Error al exportar PDF.";
            ShowNotification("Error de Exportación", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    public Task SaveJpegAsync() => SaveImagesInternalAsync(ImageExportFormat.Jpeg);

    [RelayCommand]
    public Task SavePngAsync() => SaveImagesInternalAsync(ImageExportFormat.Png);

    [RelayCommand]
    public Task SaveWebpAsync() => SaveImagesInternalAsync(ImageExportFormat.WebP);

    private async Task SaveImagesInternalAsync(ImageExportFormat format)
    {
        if (!CanSave || Pages.Count == 0) return;

        string ext = ImageSharpExportService.GetDefaultExtension(format);
        string defaultFileName = Pages.Count == 1
            ? $"Escaneo_{DateTime.Now:yyyy-MM-dd_HHmm}{ext}"
            : $"Lote_{DateTime.Now:yyyy-MM-dd_HHmm}";

        string? targetPath = await _dialogService.PickSaveImagePathAsync(defaultFileName, format);
        if (string.IsNullOrWhiteSpace(targetPath)) return;

        IsExporting = true;
        StatusMessage = $"Exportando imágenes ({format})...";

        try
        {
            var rawPages = Pages.Select(p => p.Model).ToList();
            var exportedFiles = await _imageExportService.ExportPagesAsync(rawPages, targetPath, format);

            IsDirty = false;
            _ = _autosaveService.ClearSessionAsync();
            StatusMessage = $"Imágenes guardadas exitosamente ({exportedFiles.Count} archivo{(exportedFiles.Count > 1 ? "s" : "")}).";
            ShowNotification(
                "Exportación Completada",
                exportedFiles.Count == 1
                    ? $"Se ha guardado en:\n{exportedFiles[0]}"
                    : $"Se han guardado {exportedFiles.Count} páginas en:\n{Path.GetDirectoryName(exportedFiles[0])}",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            StatusMessage = "Error al exportar imágenes.";
            ShowNotification("Error de Exportación", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    public async Task PrintDocumentAsync()
    {
        if (!CanSave || Pages.Count == 0) return;

        var printers = _printService.GetInstalledPrinters();
        if (printers == null || printers.Count == 0)
        {
            ShowNotification("Impresoras no encontradas", "No se detectaron impresoras instaladas en el equipo.", InfoBarSeverity.Warning);
            return;
        }

        if (WindowHelper.MainWindow?.Content?.XamlRoot == null)
        {
            var raw = Pages.Select(p => p.Model).ToList();
            await _printService.PrintPagesAsync(raw);
            return;
        }

        var defaultPrinter = _printService.GetDefaultPrinter();
        var dialog = new Views.PrintDocumentDialog(printers, defaultPrinter)
        {
            XamlRoot = WindowHelper.MainWindow.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var options = dialog.GetPrintOptions();
            StatusMessage = "Imprimiendo documento...";
            IsExporting = true;
            try
            {
                var rawPages = Pages.Select(p => p.Model).ToList();
                bool success = await _printService.PrintPagesAsync(rawPages, options);
                if (success)
                {
                    StatusMessage = "Documento enviado a la impresora.";
                    ShowNotification("Impresión Enviada", $"El documento se ha enviado a {options.PrinterName}.", InfoBarSeverity.Success);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al imprimir documento.";
                ShowNotification("Error de Impresión", ex.Message, InfoBarSeverity.Error);
            }
            finally
            {
                IsExporting = false;
            }
        }
    }

    [RelayCommand]
    public async Task OpenSelectedInExternalViewerAsync()
    {
        if (SelectedPage == null) return;
        await _systemService.OpenInExternalViewerAsync(SelectedPage.Model);
    }

    [RelayCommand]
    public async Task OpenDocumentInExternalViewerAsync()
    {
        if (Pages.Count == 0) return;
        var rawPages = Pages.Select(p => p.Model).ToList();
        await _systemService.OpenDocumentInExternalViewerAsync(rawPages);
    }

    [RelayCommand]
    public async Task EmailDocumentAsync()
    {
        if (Pages.Count == 0) return;
        var rawPages = Pages.Select(p => p.Model).ToList();
        await _systemService.EmailPagesAsync(rawPages);
    }

    [RelayCommand]
    public async Task ShareDocumentAsync()
    {
        if (Pages.Count == 0) return;
        var rawPages = Pages.Select(p => p.Model).ToList();
        await _systemService.SharePagesAsync(rawPages);
    }

    [RelayCommand]
    public void RotateSelectedLeft()
    {
        SelectedPage?.RotateCounterClockwise();
    }

    [RelayCommand]
    public void RotateSelectedRight()
    {
        SelectedPage?.RotateClockwise();
    }

    [RelayCommand]
    public async Task DeleteSelectedPageAsync()
    {
        if (SelectedPage == null) return;
        await DeletePageInternalAsync(SelectedPage);
    }

    [RelayCommand]
    public async Task ClearAllPagesAsync()
    {
        if (Pages.Count == 0) return;

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Limpiar documento",
            "¿Deseas descartar todas las páginas escaneadas del documento actual?",
            "Descartar",
            "Cancelar");

        if (confirmed)
        {
            Pages.Clear();
            SelectedPage = null;
            UpdatePageSummary();
            IsDirty = false;
            await _autosaveService.ClearSessionAsync();
            StatusMessage = "Documento limpiado.";
        }
    }

    [RelayCommand]
    public async Task OpenSettingsAsync()
    {
        var activeService = (SelectedScanner?.Id == MockScannerService.MockScannerId)
            ? (IScannerService)_mockScannerService
            : _scannerService;

        var caps = SelectedScanner != null
            ? await activeService.GetCapabilitiesAsync(SelectedScanner)
            : ScannerCapabilities.Default;

        var settingsVm = new ScanSettingsViewModel(_settingsService, caps);
        await settingsVm.InitializeAsync();

        if (Helpers.WindowHelper.MainWindow?.Content?.XamlRoot is Microsoft.UI.Xaml.XamlRoot xamlRoot)
        {
            var dialog = new Views.ScanSettingsDialog(settingsVm)
            {
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await settingsVm.SaveAsync();
                var saved = await _settingsService.LoadSettingsAsync();
                ContinuousScanDelaySeconds = saved.ContinuousScanDelaySeconds;

                SelectedProfile = SelectedProfile with
                {
                    Dpi = saved.CustomDpi,
                    PageFormat = saved.PaperSize
                };

                ShowNotification("Preferencias actualizadas", "Se aplicaron los nuevos ajustes de digitalización.", InfoBarSeverity.Success);
            }
        }
    }

    [RelayCommand]
    public async Task DuplicateSelectedPageAsync()
    {
        if (SelectedPage == null) return;
        int currentIndex = Pages.IndexOf(SelectedPage);
        var clonedModel = SelectedPage.Model.Clone();
        var itemVm = new ScannedPageItemViewModel(
            clonedModel,
            currentIndex + 2,
            onDeleteRequested: OnPageDeleteRequested,
            onPageUpdated: OnPageModified);

        await itemVm.InitializeThumbnailAsync();
        Pages.Insert(currentIndex + 1, itemVm);

        for (int i = 0; i < Pages.Count; i++)
        {
            Pages[i].PageNumber = i + 1;
        }

        SelectedPage = itemVm;
        UpdatePageSummary();
        IsDirty = true;
        TriggerAutosave();
        StatusMessage = $"Página duplicada.";
    }

    [RelayCommand]
    public async Task OpenReorderDialogAsync()
    {
        if (Pages.Count <= 1)
        {
            ShowNotification("Reordenar páginas", "Se necesitan al menos 2 páginas para reordenar el documento.", InfoBarSeverity.Informational);
            return;
        }

        if (Helpers.WindowHelper.MainWindow?.Content?.XamlRoot is Microsoft.UI.Xaml.XamlRoot xamlRoot)
        {
            var dialog = new Views.ReorderPagesDialog
            {
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                switch (dialog.SelectedAction)
                {
                    case Views.ReorderAction.Reverse:
                        var reversed = Helpers.DocumentReorderHelper.Reverse(Pages.ToList());
                        Pages.Clear();
                        foreach (var p in reversed) Pages.Add(p);
                        ShowNotification("Orden invertido", "Se invirtió el orden de las páginas del documento.", InfoBarSeverity.Success);
                        break;

                    case Views.ReorderAction.Collate:
                        var collated = Helpers.DocumentReorderHelper.Collate(Pages.ToList(), dialog.ReverseBackPages);
                        Pages.Clear();
                        foreach (var p in collated) Pages.Add(p);
                        ShowNotification("Caras intercaladas", "Se intercalaron los frentes y dorsos del documento.", InfoBarSeverity.Success);
                        break;

                    case Views.ReorderAction.RotateEven:
                        Helpers.DocumentReorderHelper.RotateAlternates(Pages.ToList(), evenPages: true, angleDelta: 180);
                        ShowNotification("Páginas pares rotadas", "Se rotaron 180° las páginas pares.", InfoBarSeverity.Success);
                        break;

                    case Views.ReorderAction.RotateOdd:
                        Helpers.DocumentReorderHelper.RotateAlternates(Pages.ToList(), evenPages: false, angleDelta: 180);
                        ShowNotification("Páginas impares rotadas", "Se rotaron 180° las páginas impares.", InfoBarSeverity.Success);
                        break;
                }

                SelectedPage = Pages.FirstOrDefault();
                UpdatePageSummary();
                IsDirty = true;
                TriggerAutosave();
            }
        }
    }

    [RelayCommand]
    public async Task CopySelectedPageToClipboardAsync()
    {
        if (SelectedPage == null) return;

        try
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;

            byte[] processedBytes = ImageHelper.ProcessImage(
                SelectedPage.Model.ImageBytes,
                SelectedPage.Model.RotationAngle,
                SelectedPage.Model.Crop);

            var memStream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(memStream))
            {
                writer.WriteBytes(processedBytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }
            memStream.Seek(0);

            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(memStream));
            Clipboard.SetContent(dataPackage);

            ShowNotification(
                "Página copiada al portapapeles",
                "La imagen procesada está lista para pegarse (Ctrl+V) en otras aplicaciones.",
                InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            ShowNotification("Error al copiar al portapapeles", ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void OnPageDeleteRequested(ScannedPageItemViewModel pageVm)
    {
        await DeletePageInternalAsync(pageVm);
    }

    private async Task DeletePageInternalAsync(ScannedPageItemViewModel pageVm)
    {
        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Eliminar página",
            $"¿Eliminar la Página {pageVm.PageNumber}?",
            "Eliminar",
            "Cancelar");

        if (confirmed)
        {
            int index = Pages.IndexOf(pageVm);
            Pages.Remove(pageVm);

            // Reindexar números de página
            for (int i = 0; i < Pages.Count; i++)
            {
                Pages[i].PageNumber = i + 1;
            }

            if (SelectedPage == pageVm)
            {
                SelectedPage = Pages.ElementAtOrDefault(index) ?? Pages.LastOrDefault();
            }

            UpdatePageSummary();
            IsDirty = true;
            TriggerAutosave();
            StatusMessage = "Página eliminada.";
        }
    }

    [RelayCommand]
    public async Task RecoverSessionAsync()
    {
        try
        {
            var pages = await _autosaveService.RecoverSessionAsync();
            if (pages.Count > 0)
            {
                Pages.Clear();
                foreach (var page in pages)
                {
                    await AddAcquiredPageAsync(page);
                }
                IsDirty = true;
                HasRecoverableSession = false;
                ShowNotification("Recuperación Completada", $"Se recuperaron {pages.Count} página(s) de la sesión anterior.", InfoBarSeverity.Success);
            }
            else
            {
                HasRecoverableSession = false;
                await _autosaveService.ClearSessionAsync();
            }
        }
        catch (Exception ex)
        {
            ShowNotification("Error de Recuperación", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public async Task DiscardRecoveryAsync()
    {
        HasRecoverableSession = false;
        await _autosaveService.ClearSessionAsync();
    }

    public async Task<bool> ConfirmCloseIfDirtyAsync()
    {
        if (!IsDirty || Pages.Count == 0) return true;

        bool confirm = await _dialogService.ShowConfirmationAsync(
            "Cambios sin guardar",
            "Tienes páginas digitalizadas sin guardar. ¿Deseas cerrar la aplicación y descartar los cambios?",
            "Cerrar y descartar",
            "Cancelar");

        if (confirm)
        {
            await _autosaveService.ClearSessionAsync();
            IsDirty = false;
        }

        return confirm;
    }

    private void TriggerAutosave()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _autosaveService.SaveSessionAsync(Pages.ToList());
            }
            catch { }
        });
    }

    private void UpdatePageSummary()
    {
        HasPages = Pages.Count > 0;
        PageCountSummary = Pages.Count switch
        {
            0 => "0 páginas",
            1 => "1 página",
            _ => $"{Pages.Count} páginas"
        };
        OnPropertyChanged(nameof(CanSave));
    }

    private void ShowNotification(string title, string message, InfoBarSeverity severity)
    {
        InfoBarTitle = title;
        InfoBarMessage = message;
        InfoBarSeverity = severity;
        IsInfoBarOpen = true;
    }

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint uType);

    private static void PlayBeepNotification()
    {
        try
        {
            MessageBeep(0);
        }
        catch { }
    }
}
