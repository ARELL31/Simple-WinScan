using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Simple_WinScan.Helpers;
using Simple_WinScan.Services.Interfaces;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Simple_WinScan.Services.Implementations;

public class WinUiDialogService : IDialogService
{
    public async Task<string?> PickSavePdfPathAsync(string defaultFileName)
    {
        var savePicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = defaultFileName
        };

        savePicker.FileTypeChoices.Add("Documento PDF (*.pdf)", new List<string> { ".pdf" });

        IntPtr hwnd = WindowHelper.GetWindowHandle();
        if (hwnd != IntPtr.Zero)
        {
            InitializeWithWindow.Initialize(savePicker, hwnd);
        }

        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickSaveImagePathAsync(string defaultFileName, ImageExportFormat format)
    {
        var savePicker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            SuggestedFileName = defaultFileName
        };

        switch (format)
        {
            case ImageExportFormat.Jpeg:
                savePicker.FileTypeChoices.Add("Imagen JPEG (*.jpg, *.jpeg)", new List<string> { ".jpg", ".jpeg" });
                break;
            case ImageExportFormat.Png:
                savePicker.FileTypeChoices.Add("Imagen PNG (*.png)", new List<string> { ".png" });
                break;
            case ImageExportFormat.WebP:
                savePicker.FileTypeChoices.Add("Imagen WebP (*.webp)", new List<string> { ".webp" });
                break;
        }

        IntPtr hwnd = WindowHelper.GetWindowHandle();
        if (hwnd != IntPtr.Zero)
        {
            InitializeWithWindow.Initialize(savePicker, hwnd);
        }

        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        if (WindowHelper.MainWindow?.Content?.XamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Aceptar",
            XamlRoot = WindowHelper.MainWindow.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText)
    {
        if (WindowHelper.MainWindow?.Content?.XamlRoot == null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = WindowHelper.MainWindow.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}
