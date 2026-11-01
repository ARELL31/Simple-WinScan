using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Simple_WinScan.Services.Implementations;
using Simple_WinScan.Services.Interfaces;
using Simple_WinScan.ViewModels;

namespace Simple_WinScan;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public IServiceProvider Services { get; }

    public static T GetService<T>() where T : class
    {
        if (Current is not App app || app.Services == null)
        {
            throw new InvalidOperationException("El contenedor de servicios no ha sido inicializado.");
        }
        return app.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Servicios del Dominio y Hardware
        services.AddSingleton<IScannerService, WiaScannerService>();
        services.AddSingleton<IPdfExportService, PdfSharpExportService>();
        services.AddSingleton<IDialogService, WinUiDialogService>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
