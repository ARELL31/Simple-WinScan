using Microsoft.UI.Xaml.Controls;
using Simple_WinScan.ViewModels;

namespace Simple_WinScan.Views;

public sealed partial class ScanSettingsDialog : ContentDialog
{
    public ScanSettingsViewModel ViewModel { get; }

    public ScanSettingsDialog(ScanSettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }
}
