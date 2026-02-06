namespace MicMuteNet.Models;

/// <summary>
/// Defines how the mute hotkey behaves.
/// </summary>
public enum MuteMode
{
    /// <summary>
    /// Press to toggle mute state.
    /// </summary>
    Toggle,

    /// <summary>
    /// Hold to unmute, release to mute. Microphone is muted by default.
    /// </summary>
    PushToTalk,

    /// <summary>
    /// Hold to mute, release to unmute. Microphone is unmuted by default.
    /// </summary>
    PushToMute
}
