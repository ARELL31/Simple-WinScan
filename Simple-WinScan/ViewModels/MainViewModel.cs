using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
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
    private readonly IDialogService _dialogService;
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
    public bool CanScan => !IsScanning && !IsExporting;
    public bool CanSave => HasPages && !IsScanning && !IsExporting;

    public MainViewModel(
        IScannerService scannerService,
        IPdfExportService pdfExportService,
        IDialogService dialogService)
        : this(scannerService, new MockScannerService(), pdfExportService, dialogService)
    {
    }

    public MainViewModel(
        IScannerService scannerService,
        MockScannerService mockScannerService,
        IPdfExportService pdfExportService,
        IDialogService dialogService)
    {
        _scannerService = scannerService ?? throw new ArgumentNullException(nameof(scannerService));
        _mockScannerService = mockScannerService ?? throw new ArgumentNullException(nameof(mockScannerService));
        _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        // Inicializar perfiles estándar
        AvailableProfiles.Add(ScanProfile.TextDefault);
        AvailableProfiles.Add(ScanProfile.PhotoDefault);
        _selectedProfile = AvailableProfiles[0];
    }

    public async Task InitializeAsync()
    {
        await LoadScannersAsync();
    }

    [RelayCommand]
    public async Task LoadScannersAsync()
    {
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

    [RelayCommand]
    public async Task ToggleVirtualScannerAsync()
    {
        IsVirtualScannerEnabled = !IsVirtualScannerEnabled;
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

            int nextNumber = Pages.Count + 1;
            var itemVm = new ScannedPageItemViewModel(
                scannedPage,
                nextNumber,
                onDeleteRequested: OnPageDeleteRequested,
                onPageUpdated: UpdatePageSummary);

            await itemVm.InitializeThumbnailAsync();

            Pages.Add(itemVm);
            SelectedPage = itemVm;
            UpdatePageSummary();

            StatusMessage = $"Página {nextNumber} digitalizada con éxito.";
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
    public void CancelScan()
    {
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
            StatusMessage = "Documento limpiado.";
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
            StatusMessage = "Página eliminada.";
        }
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
}
