using MicMuteNet.Models;

namespace MicMuteNet.Services;

/// <summary>
/// Service for persisting and loading application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current settings.
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    /// Loads settings from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves current settings to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Event raised when settings are changed and saved.
    /// </summary>
    event EventHandler? SettingsChanged;
}
