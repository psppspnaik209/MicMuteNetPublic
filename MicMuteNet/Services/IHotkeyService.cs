using MicMuteNet.Models;

namespace MicMuteNet.Services;

/// <summary>
/// Service for registering and handling global hotkeys.
/// </summary>
public interface IHotkeyService : IDisposable
{
    /// <summary>
    /// Event raised when the registered hotkey is pressed.
    /// </summary>
    event EventHandler? HotkeyPressed;

    /// <summary>
    /// Gets the current hotkey configuration.
    /// </summary>
    HotkeyConfiguration? CurrentHotkey { get; }

    /// <summary>
    /// Registers a global hotkey.
    /// </summary>
    /// <param name="config">The hotkey configuration.</param>
    /// <returns>True if registration succeeded.</returns>
    bool RegisterHotkey(HotkeyConfiguration config);

    /// <summary>
    /// Unregisters the current hotkey.
    /// </summary>
    void UnregisterHotkey();

    /// <summary>
    /// Gets whether a hotkey is currently registered.
    /// </summary>
    bool IsRegistered { get; }
}
