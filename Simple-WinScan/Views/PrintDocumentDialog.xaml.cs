using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Simple_WinScan.Services.Interfaces;

namespace Simple_WinScan.Views;

public sealed partial class PrintDocumentDialog : ContentDialog
{
    public PrintDocumentDialog(IReadOnlyList<string> printers, string? defaultPrinter)
    {
        this.InitializeComponent();

        PrintersComboBox.ItemsSource = printers;
        if (!string.IsNullOrEmpty(defaultPrinter) && printers.Contains(defaultPrinter))
        {
            PrintersComboBox.SelectedItem = defaultPrinter;
        }
        else if (printers.Count > 0)
        {
            PrintersComboBox.SelectedIndex = 0;
        }
    }

    public PrintOptions GetPrintOptions()
    {
        return new PrintOptions
        {
            PrinterName = PrintersComboBox.SelectedItem as string,
            Landscape = OrientationRadioButtons.SelectedIndex == 1,
            Copies = (int)Math.Max(1, CopiesNumberBox.Value)
        };
    }
}
