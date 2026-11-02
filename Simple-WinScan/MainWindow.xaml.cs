using System;
using Microsoft.UI.Xaml;
using Simple_WinScan.Helpers;
using Simple_WinScan.ViewModels;

namespace Simple_WinScan;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        ViewModel = App.GetService<MainViewModel>();
        this.InitializeComponent();

        WindowHelper.MainWindow = this;
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);

        this.Activated += MainWindow_Activated;
        this.AppWindow.Closing += AppWindow_Closing;
    }

    private bool _isClosingConfirmed;

    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_isClosingConfirmed) return;

        if (ViewModel.IsDirty && ViewModel.Pages.Count > 0)
        {
            args.Cancel = true;
            bool discard = await ViewModel.ConfirmCloseIfDirtyAsync();
            if (discard)
            {
                _isClosingConfirmed = true;
                this.Close();
            }
        }
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        this.Activated -= MainWindow_Activated;
        await ViewModel.InitializeAsync();
    }

    public Visibility GetProgressVisibility(bool isScanning, bool isExporting)
    {
        return (isScanning || isExporting) ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetCancelVisibility(bool isScanning, bool isContinuousScanActive)
    {
        return (isScanning || isContinuousScanActive) ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetEmptyStateVisibility(bool hasPages)
    {
        return !hasPages ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetDocumentStateVisibility(bool hasPages)
    {
        return hasPages ? Visibility.Visible : Visibility.Collapsed;
    }
}
