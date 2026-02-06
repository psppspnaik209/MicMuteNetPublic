namespace MicMuteNet.Models;

/// <summary>
/// Represents an audio input device (microphone).
/// </summary>
public sealed record MicrophoneDevice
{
    /// <summary>
    /// Unique device identifier from Windows audio subsystem.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Friendly name of the device.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Indicates if this is the system default input device.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Current mute state of the device.
    /// </summary>
    public bool IsMuted { get; init; }

    /// <summary>
    /// Current volume level (0.0 to 1.0).
    /// </summary>
    public float Volume { get; init; }

    public override string ToString() => Name;
}
