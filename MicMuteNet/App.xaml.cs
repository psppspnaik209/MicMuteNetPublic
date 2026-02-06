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
        InitializeComponent();
        ConfigureServices();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<IAudioDeviceService, AudioDeviceService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();

        // Register ViewModels
        services.AddTransient<MainViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();

        Services = services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = Services.GetRequiredService<MainWindow>();
        _window.Activate();
    }
}
