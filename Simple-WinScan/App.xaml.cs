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
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
                System.IO.File.WriteAllText(logPath, $"[{DateTime.Now}] AppDomain Exception: {e.ExceptionObject}");
            }
            catch { }
        };
        this.InitializeComponent();
        this.UnhandledException += App_UnhandledException;
        Services = ConfigureServices();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
            System.IO.File.WriteAllText(logPath, $"[{DateTime.Now}] Unhandled Exception: {e.Message}\n{e.Exception}");
        }
        catch { }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Servicios del Dominio y Hardware
        services.AddSingleton<IWiaNativeInterop, DefaultWiaNativeInterop>();
        services.AddSingleton<IScannerService, WiaScannerService>();
        services.AddSingleton<MockScannerService>();
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
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            try
            {
                var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
                System.IO.File.WriteAllText(logPath, $"[{DateTime.Now}] OnLaunched Exception: {ex.Message}\n{ex}");
            }
            catch { }
            throw;
        }
    }
}
