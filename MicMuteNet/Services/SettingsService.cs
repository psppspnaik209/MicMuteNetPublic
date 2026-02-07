using System.Text.Json;
using MicMuteNet.Models;

namespace MicMuteNet.Services;

/// <summary>
/// JSON-based settings service using local app data.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        try
        {
            StartupLogger.Log("SettingsService constructing...");
            var settingsDirectory = AppContext.BaseDirectory;
            StartupLogger.Log($"Settings directory: {settingsDirectory}");
            
            if (!Directory.Exists(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
                StartupLogger.Log("Settings directory created.");
            }

            _settingsPath = Path.Combine(settingsDirectory, SettingsFileName);
            StartupLogger.Log($"Settings path: {_settingsPath}");
            Settings = new AppSettings();
            StartupLogger.Log("SettingsService constructed successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"ERROR in SettingsService constructor: {ex}");
            throw;
        }
    }

    public AppSettings Settings { get; private set; }

    public string SettingsPath => _settingsPath;

    public event EventHandler? SettingsChanged;

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    Settings = settings;
                }
            }
            else
            {
                await SaveAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            Settings = new AppSettings();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(Settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
}
