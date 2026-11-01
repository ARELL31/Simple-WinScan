using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Simple_WinScan.Helpers;
using Simple_WinScan.Models;

namespace Simple_WinScan.ViewModels;

public partial class ScannedPageItemViewModel : ObservableObject
{
    private readonly Action<ScannedPageItemViewModel>? _onDeleteRequested;
    private readonly Action? _onPageUpdated;

    public ScannedPage Model { get; }

    [ObservableProperty]
    private int _pageNumber;

    [ObservableProperty]
    private int _rotationAngle;

    [ObservableProperty]
    private BitmapImage? _thumbnailImage;

    [ObservableProperty]
    private bool _isSelected;

    public ScannedPageItemViewModel(ScannedPage model, int pageNumber, Action<ScannedPageItemViewModel>? onDeleteRequested = null, Action? onPageUpdated = null)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        _pageNumber = pageNumber;
        _rotationAngle = model.RotationAngle;
        _onDeleteRequested = onDeleteRequested;
        _onPageUpdated = onPageUpdated;
    }

    public async Task InitializeThumbnailAsync()
    {
        ThumbnailImage = await ImageHelper.CreateBitmapImageAsync(Model.ImageBytes);
    }

    [RelayCommand]
    public void RotateClockwise()
    {
        Model.RotateClockwise();
        RotationAngle = Model.RotationAngle;
        _onPageUpdated?.Invoke();
    }

    [RelayCommand]
    public void RotateCounterClockwise()
    {
        Model.RotateCounterClockwise();
        RotationAngle = Model.RotationAngle;
        _onPageUpdated?.Invoke();
    }

    [RelayCommand]
    public void RequestDelete()
    {
        _onDeleteRequested?.Invoke(this);
    }
}
