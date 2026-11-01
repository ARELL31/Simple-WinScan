using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Simple_WinScan.ViewModels;

namespace Simple_WinScan.Controls;

public sealed partial class PageCardControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ScannedPageItemViewModel),
            typeof(PageCardControl),
            new PropertyMetadata(null));

    public ScannedPageItemViewModel? ViewModel
    {
        get => (ScannedPageItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public PageCardControl()
    {
        this.InitializeComponent();
    }

    public string GetPageLabel(int pageNumber) => $"Página {pageNumber}";

    public bool IsThumbnailLoading(BitmapImage? image) => image == null;
}
