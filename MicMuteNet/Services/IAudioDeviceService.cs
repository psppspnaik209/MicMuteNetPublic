using MicMuteNet.Models;

namespace MicMuteNet.Services;

/// <summary>
/// Service for managing audio input devices and mute state.
/// </summary>
public interface IAudioDeviceService : IDisposable
{
    /// <summary>
    /// Gets the list of available microphone devices.
    /// </summary>
    IReadOnlyList<MicrophoneDevice> Devices { get; }

    /// <summary>
    /// Gets the currently selected device ID.
    /// </summary>
    string? SelectedDeviceId { get; }

    /// <summary>
    /// Gets whether the selected device is currently muted.
    /// </summary>
    bool IsMuted { get; }

    /// <summary>
    /// Gets the current volume of the selected device (0.0 to 1.0).
    /// </summary>
    float Volume { get; }

    /// <summary>
    /// Event raised when the mute state changes.
    /// </summary>
    event EventHandler<bool>? MuteStateChanged;

    /// <summary>
    /// Event raised when the device list changes.
    /// </summary>
    event EventHandler? DevicesChanged;

    /// <summary>
    /// Refreshes the list of available devices.
    /// </summary>
    void RefreshDevices();

    /// <summary>
    /// Selects a microphone device by ID.
    /// </summary>
    /// <param name="deviceId">The device ID to select, or null for default device.</param>
    void SelectDevice(string? deviceId);

    /// <summary>
    /// Sets the mute state of the selected device.
    /// </summary>
    /// <param name="muted">True to mute, false to unmute.</param>
    /// <returns>True if successful.</returns>
    bool SetMute(bool muted);

    /// <summary>
    /// Toggles the mute state of the selected device.
    /// </summary>
    /// <returns>The new mute state.</returns>
    bool ToggleMute();

    /// <summary>
    /// Sets the volume of the selected device.
    /// </summary>
    /// <param name="volume">Volume level from 0.0 to 1.0.</param>
    /// <returns>True if successful.</returns>
    bool SetVolume(float volume);
}
