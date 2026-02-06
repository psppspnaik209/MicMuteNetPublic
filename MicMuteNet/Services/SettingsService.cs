using System.Text.Json;
using MicMuteNet.Models;
using Windows.Storage;

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
        var localFolder = ApplicationData.Current.LocalFolder.Path;
        _settingsPath = Path.Combine(localFolder, SettingsFileName);
        Settings = new AppSettings();
    }

    public AppSettings Settings { get; private set; }

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
