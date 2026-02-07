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
    public bool OverlayEnabled { get; set; } = false;
    public double OverlayDuration { get; set; } = 2.0; // Duration in seconds
    public OverlayVisibilityMode OverlayVisibility { get; set; } = OverlayVisibilityMode.Hidden;
    public double OverlayOpacity { get; set; } = 1.0;
    public double OverlayX { get; set; }
    public double OverlayY { get; set; }

    // Notification settings
    public bool NotificationEnabled { get; set; } = true;
    public float NotificationVolume { get; set; } = 0.5f;
    public int OutputDeviceNumber { get; set; } = -1; // -1 = default device

    // Startup settings
    public bool RunAtStartup { get; set; }
    public bool StartMinimized { get; set; }
    public bool MinimizeOnClose { get; set; } = true;
    public bool DefaultMuteOnStartup { get; set; } = false;
    public bool CollectLogs { get; set; } = 
#if DEBUG
        true;  // Debug builds default to logging enabled
#else
        false; // Release builds default to logging disabled
#endif

    // Window state
    public double? WindowX { get; set; }
    public double? WindowY { get; set; }
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
}
