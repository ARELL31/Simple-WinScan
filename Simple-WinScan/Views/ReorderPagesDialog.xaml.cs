using Microsoft.UI.Xaml.Controls;

namespace Simple_WinScan.Views;

public enum ReorderAction
{
    Reverse,
    Collate,
    RotateEven,
    RotateOdd
}

public sealed partial class ReorderPagesDialog : ContentDialog
{
    public ReorderAction SelectedAction
    {
        get
        {
            if (RadioCollate.IsChecked == true) return ReorderAction.Collate;
            if (RadioRotateEven.IsChecked == true) return ReorderAction.RotateEven;
            if (RadioRotateOdd.IsChecked == true) return ReorderAction.RotateOdd;
            return ReorderAction.Reverse;
        }
    }

    public bool ReverseBackPages => CheckReverseBackPages.IsChecked == true;

    public ReorderPagesDialog()
    {
        this.InitializeComponent();
    }
}
