namespace MicMuteNet.Models;

/// <summary>
/// Defines when the overlay should be visible.
/// </summary>
public enum OverlayVisibilityMode
{
    /// <summary>
    /// Overlay is always visible.
    /// </summary>
    Always,

    /// <summary>
    /// Overlay is visible only when muted.
    /// </summary>
    WhenMuted,

    /// <summary>
    /// Overlay is visible only when unmuted.
    /// </summary>
    WhenUnmuted,

    /// <summary>
    /// Overlay is never visible.
    /// </summary>
    Hidden
}

/// <summary>
/// Application settings persisted to disk.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// ID of the selected microphone device.
    /// </summary>
    public string? SelectedDeviceId { get; set; }

    /// <summary>
    /// Current mute mode.
    /// </summary>
    public MuteMode MuteMode { get; set; } = MuteMode.Toggle;

    /// <summary>
    /// Initial mute state on startup.
    /// </summary>
    public bool? InitialMuteState { get; set; }

    /// <summary>
    /// Volume control enabled.
    /// </summary>
    public bool VolumeControlEnabled { get; set; }

    /// <summary>
    /// Primary hotkey configuration.
    /// </summary>
    public HotkeyConfiguration Hotkey { get; set; } = new();

    /// <summary>
    /// Alternate hotkey configuration.
    /// </summary>
    public HotkeyConfiguration? HotkeyAlt { get; set; }

    // Overlay settings
    public OverlayVisibilityMode OverlayVisibility { get; set; } = OverlayVisibilityMode.Hidden;
    public double OverlayOpacity { get; set; } = 1.0;
    public double OverlayX { get; set; }
    public double OverlayY { get; set; }

    // Notification settings
    public bool NotificationsEnabled { get; set; } = true;
    public string MuteSound { get; set; } = "Beep300";
    public string UnmuteSound { get; set; } = "Beep750";
    public float NotificationVolume { get; set; } = 1.0f;
    public string? OutputDeviceId { get; set; }

    // Startup settings
    public bool RunAtStartup { get; set; }
    public bool StartMinimized { get; set; }
    public bool MinimizeOnClose { get; set; } = true;

    // Window state
    public double? WindowX { get; set; }
    public double? WindowY { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
}
