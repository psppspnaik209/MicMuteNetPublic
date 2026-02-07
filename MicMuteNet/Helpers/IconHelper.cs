namespace MicMuteNet.Helpers;

/// <summary>
/// Provides centralized access to application icon paths.
/// </summary>
public static class IconHelper
{
    private static readonly string IconsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons");

    /// <summary>
    /// Gets the path to the microphone enabled icon.
    /// </summary>
    public static string MicrophoneEnabledIconPath => Path.Combine(IconsDirectory, "microphoneEnabled.ico");

    /// <summary>
    /// Gets the path to the microphone disabled icon.
    /// </summary>
    public static string MicrophoneDisabledIconPath => Path.Combine(IconsDirectory, "microphoneDisabled.ico");

    /// <summary>
    /// Gets the icon path based on mute state.
    /// </summary>
    /// <param name="isMuted">True if microphone is muted, false otherwise.</param>
    /// <returns>Path to the appropriate icon file.</returns>
    public static string GetIconPath(bool isMuted) => isMuted ? MicrophoneDisabledIconPath : MicrophoneEnabledIconPath;

    /// <summary>
    /// Checks if the specified icon file exists.
    /// </summary>
    /// <param name="iconPath">Path to the icon file.</param>
    /// <returns>True if the icon file exists, false otherwise.</returns>
    public static bool IconExists(string iconPath) => File.Exists(iconPath);
}
