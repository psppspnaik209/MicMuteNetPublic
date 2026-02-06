namespace MicMuteNet.Services;

/// <summary>
/// Service for playing audio notification sounds.
/// </summary>
public interface INotificationService : IDisposable
{
    /// <summary>
    /// Gets or sets whether notifications are enabled.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the notification volume (0.0 to 1.0).
    /// </summary>
    float Volume { get; set; }

    /// <summary>
    /// Plays the muted notification sound.
    /// </summary>
    void PlayMutedSound();

    /// <summary>
    /// Plays the unmuted notification sound.
    /// </summary>
    void PlayUnmutedSound();
}
