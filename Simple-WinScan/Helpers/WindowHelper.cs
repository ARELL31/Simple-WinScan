using System;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Simple_WinScan.Helpers;

public static class WindowHelper
{
    public static Window? MainWindow { get; set; }

    public static IntPtr GetWindowHandle()
    {
        if (MainWindow == null) return IntPtr.Zero;
        return WindowNative.GetWindowHandle(MainWindow);
    }
}
