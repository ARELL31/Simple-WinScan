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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCrop))]
    private CropRectangle? _crop;

    [ObservableProperty]
    private bool _isCropOverlayVisible;

    public bool HasCrop => Crop != null && !Crop.IsFullImage;

    public ScannedPageItemViewModel(ScannedPage model, int pageNumber, Action<ScannedPageItemViewModel>? onDeleteRequested = null, Action? onPageUpdated = null)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        _pageNumber = pageNumber;
        _rotationAngle = model.RotationAngle;
        _crop = model.Crop;
        _onDeleteRequested = onDeleteRequested;
        _onPageUpdated = onPageUpdated;
    }

    public async Task InitializeThumbnailAsync()
    {
        ThumbnailImage = await ImageHelper.CreateBitmapImageAsync(Model.ImageBytes);
    }

    [RelayCommand]
    public void ToggleCropOverlay()
    {
        IsCropOverlayVisible = !IsCropOverlayVisible;
        if (IsCropOverlayVisible && Crop == null)
        {
            Crop = new CropRectangle(0.05, 0.05, 0.9, 0.9);
            Model.Crop = Crop;
            _onPageUpdated?.Invoke();
        }
    }

    [RelayCommand]
    public void ApplyCropPreset(string presetName)
    {
        if (Enum.TryParse<CropPreset>(presetName, true, out var preset))
        {
            double pageAspect = (Model.PixelHeight > 0)
                ? (double)Model.PixelWidth / Model.PixelHeight
                : (210.0 / 297.0);

            Crop = CropRectangle.CreatePreset(preset, pageAspect);
            Model.Crop = Crop;
            _onPageUpdated?.Invoke();
        }
    }

    [RelayCommand]
    public void ResetCrop()
    {
        Crop = null;
        Model.Crop = null;
        IsCropOverlayVisible = false;
        _onPageUpdated?.Invoke();
    }

    public void MoveCrop(double dx, double dy)
    {
        if (Crop == null) Crop = new CropRectangle(0.05, 0.05, 0.9, 0.9);
        Crop = Crop.Move(dx, dy);
        Model.Crop = Crop;
        _onPageUpdated?.Invoke();
    }

    public void ResizeCrop(double dw, double dh)
    {
        if (Crop == null) Crop = new CropRectangle(0.05, 0.05, 0.9, 0.9);
        Crop = Crop.Resize(dw, dh);
        Model.Crop = Crop;
        _onPageUpdated?.Invoke();
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
