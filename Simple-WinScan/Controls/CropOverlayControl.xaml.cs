using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Simple_WinScan.Models;
using Simple_WinScan.ViewModels;

namespace Simple_WinScan.Controls;

public sealed partial class CropOverlayControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ScannedPageItemViewModel),
            typeof(CropOverlayControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public ScannedPageItemViewModel? ViewModel
    {
        get => (ScannedPageItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private bool _isDragging;
    private bool _isResizing;
    private Windows.Foundation.Point _lastPoint;

    public CropOverlayControl()
    {
        this.InitializeComponent();
        this.SizeChanged += (s, e) => UpdateOverlayLayout();
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CropOverlayControl control)
        {
            control.UpdateOverlayLayout();
        }
    }

    public void UpdateOverlayLayout()
    {
        if (ViewModel?.Crop == null || ActualWidth <= 0 || ActualHeight <= 0) return;

        var crop = ViewModel.Crop;
        double x = crop.X * ActualWidth;
        double y = crop.Y * ActualHeight;
        double w = crop.Width * ActualWidth;
        double h = crop.Height * ActualHeight;

        Canvas.SetLeft(CropBoxBorder, x);
        Canvas.SetTop(CropBoxBorder, y);
        CropBoxBorder.Width = Math.Max(10, w);
        CropBoxBorder.Height = Math.Max(10, h);

        Canvas.SetLeft(HandleTopLeft, x - 6);
        Canvas.SetTop(HandleTopLeft, y - 6);

        Canvas.SetLeft(HandleTopRight, x + w - 6);
        Canvas.SetTop(HandleTopRight, y - 6);

        Canvas.SetLeft(HandleBottomLeft, x - 6);
        Canvas.SetTop(HandleBottomLeft, y + h - 6);

        Canvas.SetLeft(HandleBottomRight, x + w - 6);
        Canvas.SetTop(HandleBottomRight, y + h - 6);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (ViewModel == null) return;

        bool isShift = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        bool isCtrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

        double step = isCtrl ? 0.005 : 0.02;

        if (isShift)
        {
            switch (e.Key)
            {
                case VirtualKey.Right:
                    ViewModel.ResizeCrop(step, 0);
                    e.Handled = true;
                    break;
                case VirtualKey.Left:
                    ViewModel.ResizeCrop(-step, 0);
                    e.Handled = true;
                    break;
                case VirtualKey.Down:
                    ViewModel.ResizeCrop(0, step);
                    e.Handled = true;
                    break;
                case VirtualKey.Up:
                    ViewModel.ResizeCrop(0, -step);
                    e.Handled = true;
                    break;
            }
        }
        else
        {
            switch (e.Key)
            {
                case VirtualKey.Right:
                    ViewModel.MoveCrop(step, 0);
                    e.Handled = true;
                    break;
                case VirtualKey.Left:
                    ViewModel.MoveCrop(-step, 0);
                    e.Handled = true;
                    break;
                case VirtualKey.Down:
                    ViewModel.MoveCrop(0, step);
                    e.Handled = true;
                    break;
                case VirtualKey.Up:
                    ViewModel.MoveCrop(0, -step);
                    e.Handled = true;
                    break;
            }
        }

        UpdateOverlayLayout();
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(this).Position;
        _lastPoint = pt;

        if (ViewModel?.Crop == null) return;

        double x = ViewModel.Crop.X * ActualWidth;
        double y = ViewModel.Crop.Y * ActualHeight;
        double w = ViewModel.Crop.Width * ActualWidth;
        double h = ViewModel.Crop.Height * ActualHeight;

        // Si el puntero toca la esquina inferior derecha, iniciar redimensionamiento
        if (Math.Abs(pt.X - (x + w)) < 20 && Math.Abs(pt.Y - (y + h)) < 20)
        {
            _isResizing = true;
            this.CapturePointer(e.Pointer);
            e.Handled = true;
        }
        else if (pt.X >= x && pt.X <= x + w && pt.Y >= y && pt.Y <= y + h)
        {
            _isDragging = true;
            this.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel?.Crop == null || (!_isDragging && !_isResizing) || ActualWidth <= 0 || ActualHeight <= 0) return;

        var currentPt = e.GetCurrentPoint(this).Position;
        double deltaX = (currentPt.X - _lastPoint.X) / ActualWidth;
        double deltaY = (currentPt.Y - _lastPoint.Y) / ActualHeight;
        _lastPoint = currentPt;

        if (_isDragging)
        {
            ViewModel.MoveCrop(deltaX, deltaY);
        }
        else if (_isResizing)
        {
            ViewModel.ResizeCrop(deltaX, deltaY);
        }

        UpdateOverlayLayout();
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        this.ReleasePointerCapture(e.Pointer);
    }

    private void OnPresetA4Clicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyCropPreset("A4");
        UpdateOverlayLayout();
    }

    private void OnPresetLetterClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyCropPreset("Letter");
        UpdateOverlayLayout();
    }

    private void OnPresetPhotoClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyCropPreset("Photo4x6");
        UpdateOverlayLayout();
    }

    private void OnPresetCardClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyCropPreset("BusinessCard");
        UpdateOverlayLayout();
    }

    private void OnResetClicked(object sender, RoutedEventArgs e)
    {
        ViewModel?.ResetCrop();
        UpdateOverlayLayout();
    }
}
