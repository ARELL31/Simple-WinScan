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

    public Visibility GetEmptyStateVisibility(bool hasPages)
    {
        return !hasPages ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility GetDocumentStateVisibility(bool hasPages)
    {
        return hasPages ? Visibility.Visible : Visibility.Collapsed;
    }
}
