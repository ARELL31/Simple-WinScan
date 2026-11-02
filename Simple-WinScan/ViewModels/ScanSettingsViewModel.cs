using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Simple_WinScan.Models;
using Simple_WinScan.Services.Interfaces;

namespace Simple_WinScan.ViewModels;

public partial class ScanSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public ScannerCapabilities Capabilities { get; }

    [ObservableProperty]
    private int _brightness;

    [ObservableProperty]
    private int _contrast;

    [ObservableProperty]
    private int _selectedDpi = 300;

    [ObservableProperty]
    private PageSizeFormat _selectedPaperSize = PageSizeFormat.A4;

    [ObservableProperty]
    private int _continuousDelaySeconds = 5;

    [ObservableProperty]
    private string _postprocessingCommand = string.Empty;

    [ObservableProperty]
    private bool _isPostprocessingEnabled;

    [ObservableProperty]
    private bool _isOcrEnabled = true;

    public IReadOnlyList<int> AvailableDpis => Capabilities.SupportedDpis;
    public IReadOnlyList<PageSizeFormat> AvailablePaperSizes => Capabilities.SupportedPaperSizes;

    public ScanSettingsViewModel(ISettingsService settingsService, ScannerCapabilities? capabilities = null)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        Capabilities = capabilities ?? ScannerCapabilities.Default;
    }

    public async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        Brightness = settings.Brightness;
        Contrast = settings.Contrast;
        SelectedDpi = settings.CustomDpi > 0 ? settings.CustomDpi : 300;
        SelectedPaperSize = settings.PaperSize;
        ContinuousDelaySeconds = settings.ContinuousScanDelaySeconds > 0 ? settings.ContinuousScanDelaySeconds : 5;
        PostprocessingCommand = settings.PostprocessingCommand ?? string.Empty;
        IsPostprocessingEnabled = settings.IsPostprocessingEnabled;
        IsOcrEnabled = settings.IsOcrEnabled;
    }

    [RelayCommand]
    public void ResetBrightnessContrast()
    {
        Brightness = 0;
        Contrast = 0;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();
        settings.Brightness = Brightness;
        settings.Contrast = Contrast;
        settings.CustomDpi = SelectedDpi;
        settings.PaperSize = SelectedPaperSize;
        settings.ContinuousScanDelaySeconds = ContinuousDelaySeconds;
        settings.PostprocessingCommand = PostprocessingCommand;
        settings.IsPostprocessingEnabled = IsPostprocessingEnabled;
        settings.IsOcrEnabled = IsOcrEnabled;

        await _settingsService.SaveSettingsAsync(settings);
    }
}
