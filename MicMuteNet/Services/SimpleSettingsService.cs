using System.Collections.Generic;
using System.IO;
using System.Linq;
using MicMuteNet.Models;

namespace MicMuteNet.Services;

/// <summary>
/// Simple settings service using key-value file format.
/// Settings file is stored in the same directory as the executable.
/// </summary>
public sealed class SimpleSettingsService : ISettingsService
{
    private const string SettingsFileName = "MicMuteNet.settings";
    private readonly string _settingsPath;
    private readonly Dictionary<string, string> _settingsData = new();

    public SimpleSettingsService()
    {
        var settingsDirectory = AppContext.BaseDirectory;
        _settingsPath = Path.Combine(settingsDirectory, SettingsFileName);
        Settings = new AppSettings();
        StartupLogger.Log($"Settings path: {_settingsPath}");
    }

    public AppSettings Settings { get; private set; }
    public string SettingsPath => _settingsPath;
    public event EventHandler? SettingsChanged;

    public Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                StartupLogger.Log("Loading settings from file");
                var lines = File.ReadAllLines(_settingsPath);
                _settingsData.Clear();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        _settingsData[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                // Map to AppSettings object
                Settings = new AppSettings
                {
                    SelectedDeviceId = GetValue("SelectedDeviceId"),
                    MuteMode = GetEnumValue<MuteMode>("MuteMode", MuteMode.Toggle),
                    VolumeControlEnabled = GetBoolValue("VolumeControlEnabled", false),
                    OverlayEnabled = GetBoolValue("OverlayEnabled", false),
                    OverlayDuration = GetDoubleValue("OverlayDuration", 2.0),
                    OverlayOpacity = GetDoubleValue("OverlayOpacity", 1.0),
                    NotificationEnabled = GetBoolValue("NotificationEnabled", true),
                    NotificationVolume = GetFloatValue("NotificationVolume", 0.5f),
                    OutputDeviceNumber = GetIntValue("OutputDeviceNumber", -1),
                    RunAtStartup = GetBoolValue("RunAtStartup", false),
                    StartMinimized = GetBoolValue("StartMinimized", false),
                    MinimizeOnClose = GetBoolValue("MinimizeOnClose", true),
                    DefaultMuteOnStartup = GetBoolValue("DefaultMuteOnStartup", false),
                    Hotkey = new HotkeyConfiguration
                    {
                        Key = (Windows.System.VirtualKey)GetIntValue("HotkeyKey", 0),
                        Alt = GetBoolValue("HotkeyAlt", false),
                        Control = GetBoolValue("HotkeyControl", false),
                        Shift = GetBoolValue("HotkeyShift", false),
                        Win = GetBoolValue("HotkeyWin", false),
                        Suppress = GetBoolValue("HotkeySuppress", true),
                        IgnoreModifiers = GetBoolValue("HotkeyIgnoreModifiers", false)
                    }
                };

                StartupLogger.Log($"Settings loaded. SelectedDeviceId: {Settings.SelectedDeviceId ?? "none"}");
            }
            else
            {
                StartupLogger.Log("No settings file found, using defaults");
                Settings = new AppSettings();
            }
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"Error loading settings: {ex.Message}");
            Settings = new AppSettings();
        }

        return Task.CompletedTask;
    }

    public Task SaveAsync()
    {
        try
        {
            var lines = new List<string>
            {
                "# MicMuteNet Settings",
                $"SelectedDeviceId={Settings.SelectedDeviceId ?? ""}",
                $"MuteMode={Settings.MuteMode}",
                $"VolumeControlEnabled={Settings.VolumeControlEnabled}",
                $"OverlayEnabled={Settings.OverlayEnabled}",
                $"OverlayDuration={Settings.OverlayDuration}",
                $"OverlayOpacity={Settings.OverlayOpacity}",
                $"NotificationEnabled={Settings.NotificationEnabled}",
                $"NotificationVolume={Settings.NotificationVolume}",
                $"OutputDeviceNumber={Settings.OutputDeviceNumber}",
                $"RunAtStartup={Settings.RunAtStartup}",
                $"StartMinimized={Settings.StartMinimized}",
                $"MinimizeOnClose={Settings.MinimizeOnClose}",
                $"DefaultMuteOnStartup={Settings.DefaultMuteOnStartup}",
                $"CollectLogs={Settings.CollectLogs}",
                $"HotkeyKey={(int)Settings.Hotkey.Key}",
                $"HotkeyAlt={Settings.Hotkey.Alt}",
                $"HotkeyControl={Settings.Hotkey.Control}",
                $"HotkeyShift={Settings.Hotkey.Shift}",
                $"HotkeyWin={Settings.Hotkey.Win}",
                $"HotkeySuppress={Settings.Hotkey.Suppress}",
                $"HotkeyIgnoreModifiers={Settings.Hotkey.IgnoreModifiers}"
            };

            File.WriteAllLines(_settingsPath, lines);
            StartupLogger.Log($"Settings saved: {_settingsPath}");

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"Error saving settings: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private string? GetValue(string key) =>
        _settingsData.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private bool GetBoolValue(string key, bool defaultValue) =>
        _settingsData.TryGetValue(key, out var value) && bool.TryParse(value, out var result) ? result : defaultValue;

    private int GetIntValue(string key, int defaultValue) =>
        _settingsData.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : defaultValue;

    private float GetFloatValue(string key, float defaultValue) =>
        _settingsData.TryGetValue(key, out var value) && float.TryParse(value, out var result) ? result : defaultValue;

    private double GetDoubleValue(string key, double defaultValue) =>
        _settingsData.TryGetValue(key, out var value) && double.TryParse(value, out var result) ? result : defaultValue;

    private T GetEnumValue<T>(string key, T defaultValue) where T : struct, Enum =>
        _settingsData.TryGetValue(key, out var value) && Enum.TryParse<T>(value, out var result) ? result : defaultValue;
}
