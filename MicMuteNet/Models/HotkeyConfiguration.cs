using System.Text.Json.Serialization;
using Windows.System;

namespace MicMuteNet.Models;

/// <summary>
/// Represents a global hotkey configuration.
/// </summary>
public sealed class HotkeyConfiguration
{
    /// <summary>
    /// The virtual key code for the hotkey.
    /// </summary>
    public VirtualKey Key { get; set; } = VirtualKey.None;

    /// <summary>
    /// Whether the Alt modifier is required.
    /// </summary>
    public bool Alt { get; set; }

    /// <summary>
    /// Whether the Control modifier is required.
    /// </summary>
    public bool Control { get; set; }

    /// <summary>
    /// Whether the Shift modifier is required.
    /// </summary>
    public bool Shift { get; set; }

    /// <summary>
    /// Whether the Windows key modifier is required.
    /// </summary>
    public bool Win { get; set; }

    /// <summary>
    /// Suppress the hotkey from being passed to other applications.
    /// </summary>
    public bool Suppress { get; set; } = true;

    /// <summary>
    /// Ignore modifier keys (accept hotkey with any modifiers).
    /// </summary>
    public bool IgnoreModifiers { get; set; }

    [JsonIgnore]
    public bool IsEmpty => Key == VirtualKey.None;

    public override string ToString()
    {
        if (IsEmpty) return "None";

        var parts = new List<string>();
        if (Control) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Win) parts.Add("Win");
        parts.Add(Key.ToString());

        return string.Join(" + ", parts);
    }
}
