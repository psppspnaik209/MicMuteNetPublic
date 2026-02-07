using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MicMuteNet.Services;
using MicMuteNet.ViewModels;

namespace MicMuteNet;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets the current application instance.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        try
        {
            StartupLogger.Log("App() constructor starting...");
            
            StartupLogger.Log("Calling InitializeComponent()...");
            try
            {
                InitializeComponent();
            }
            catch (Exception initEx)
            {
                StartupLogger.Log($"InitializeComponent() threw exception: {initEx.GetType().FullName}");
                StartupLogger.Log($"Message: {initEx.Message}");
                StartupLogger.Log($"HResult: 0x{initEx.HResult:X8}");
                if (initEx.InnerException != null)
                {
                    StartupLogger.Log($"Inner: {initEx.InnerException.GetType().FullName} - {initEx.InnerException.Message}");
                }
                StartupLogger.Log($"Stack: {initEx.StackTrace}");
                throw;
            }
            StartupLogger.Log("InitializeComponent() completed.");
            
            StartupLogger.Log("Calling ConfigureServices()...");
            ConfigureServices();
            StartupLogger.Log("ConfigureServices() completed.");

            UnhandledException += (_, e) =>
            {
                LogException("WinUI unhandled exception", e.Exception);
                e.Handled = true;
            };
            
            StartupLogger.Log("App constructed successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"FATAL ERROR in App() constructor: {ex}");
            LogException("App constructor failure", ex);
            throw;
        }
    }

    private static void ConfigureServices()
    {
        try
        {
            StartupLogger.Log("Creating ServiceCollection...");
            var services = new ServiceCollection();

        // Register services
        StartupLogger.Log("Registering services...");
        services.AddSingleton<IAudioDeviceService, AudioDeviceService>();
        services.AddSingleton<ISettingsService, SimpleSettingsService>(); // Use simple settings service
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<INotificationService, NotificationService>();

            // Register ViewModels
            StartupLogger.Log("Registering ViewModels...");
            services.AddTransient<MainViewModel>();

            // Register Views (but NOT OverlayWindow - created lazily in MainWindow)
            StartupLogger.Log("Registering Views...");
            services.AddTransient<MainWindow>();

            StartupLogger.Log("Building ServiceProvider...");
            Services = services.BuildServiceProvider();
            StartupLogger.Log("ServiceProvider built successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"FATAL ERROR in ConfigureServices(): {ex}");
            throw;
        }
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            StartupLogger.Log("OnLaunched invoked.");
            _window = Services.GetRequiredService<MainWindow>();
            _window.Activate();
            StartupLogger.Log("Main window activated.");
        }
        catch (Exception ex)
        {
            LogException("OnLaunched failure", ex);
            throw;
        }
    }

    private static void LogException(string context, Exception exception)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "MicMuteNet.startup.log");
            var line = $"{DateTimeOffset.Now:O} {context}: {exception}";
            File.AppendAllText(logPath, line + Environment.NewLine);
            
            // Also log inner exceptions
            var inner = exception.InnerException;
            while (inner != null)
            {
                var innerLine = $"{DateTimeOffset.Now:O} Inner exception: {inner}";
                File.AppendAllText(logPath, innerLine + Environment.NewLine);
                inner = inner.InnerException;
            }
        }
        catch
        {
        }
    }
}
