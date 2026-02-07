using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using MicMuteNet.Helpers;
using Windows.Graphics;
using WinRT.Interop;

namespace MicMuteNet;

/// <summary>
/// Overlay window that shows mute status.
/// Positioned at center-bottom of primary monitor.
/// </summary>
public sealed partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private bool _isMuted;
    private DispatcherTimer? _hideTimer;
    private double _opacity = 0.9;

    public OverlayWindow()
    {
        try
        {
            StartupLogger.Log("OverlayWindow constructing...");
            InitializeComponent();
            StartupLogger.Log("OverlayWindow InitializeComponent() completed.");
            
            Title = "MicMuteNet Overlay";

            // Configure after content loads
            if (Content is FrameworkElement root)
            {
                root.Loaded += OnLoaded;
            }
            
            StartupLogger.Log("OverlayWindow constructed successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"ERROR in OverlayWindow constructor: {ex}");
            throw;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set up hide timer - must be done after window is loaded to ensure DispatcherQueue is available
        _hideTimer = new DispatcherTimer();
        _hideTimer.Interval = TimeSpan.FromSeconds(2);
        _hideTimer.Tick += (s, e) =>
        {
            _hideTimer.Stop();
            HideOverlay();
        };
        
        SetWindowIcon();
        ConfigureOverlayWindow();
    }

    private void SetWindowIcon()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            var iconPath = IconHelper.MicrophoneEnabledIconPath;
            if (IconHelper.IconExists(iconPath))
            {
                appWindow.SetIcon(iconPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set overlay window icon: {ex.Message}");
        }
    }

    private void ConfigureOverlayWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Set size - compact
        appWindow.Resize(new SizeInt32(260, 80));
        
        // Position at center-bottom of primary monitor
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var x = (displayArea.WorkArea.Width - 260) / 2;
        var y = displayArea.WorkArea.Height - 120; // 120px from bottom
        appWindow.Move(new PointInt32(x, y));

        // Make window click-through, toolwindow (no taskbar), and no-activate
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

        // Set always on top and remove title bar
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(false, false);
        }
        
        // Initially hidden
        appWindow.Hide();
    }

    /// <summary>
    /// Sets the overlay opacity (0.0 to 1.0).
    /// </summary>
    public void SetOpacity(double opacity)
    {
        _opacity = Math.Clamp(opacity, 0.2, 1.0);
        UpdateVisuals();
    }

    public void ShowMuteStatus(bool isMuted, double durationSeconds = 2.0)
    {
        _isMuted = isMuted;

        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateVisuals();

            // Show window
            ShowOverlay();

            // Auto-hide after duration
            _hideTimer?.Stop();
            if (_hideTimer != null)
            {
                _hideTimer.Interval = TimeSpan.FromSeconds(Math.Clamp(durationSeconds, 0.1, 5.0));
                _hideTimer.Start();
            }
        });
    }

    private void UpdateVisuals()
    {
        // Calculate alpha values based on opacity
        byte bgAlpha = (byte)(_opacity * 240); // Background alpha
        byte borderAlpha = (byte)(_opacity * 180); // Border alpha
        byte iconBgAlpha = (byte)(_opacity * 100); // Icon background alpha

        if (_isMuted)
        {
            // Muted - Red theme
            MuteIcon.Glyph = "\uEE56"; // Muted icon
            StatusText.Text = "MUTED";
            SubStatusText.Text = "Microphone is muted";
            
            OverlayBorder.Background = new SolidColorBrush(
                Windows.UI.Color.FromArgb(bgAlpha, 100, 30, 30));
            OverlayBorder.BorderBrush = new SolidColorBrush(
                Windows.UI.Color.FromArgb(borderAlpha, 255, 100, 100));
            IconBackground.Fill = new SolidColorBrush(
                Windows.UI.Color.FromArgb(iconBgAlpha, 255, 80, 80));
        }
        else
        {
            // Unmuted - Green/Dark theme
            MuteIcon.Glyph = "\uE720"; // Microphone icon
            StatusText.Text = "UNMUTED";
            SubStatusText.Text = "Microphone is active";
            
            OverlayBorder.Background = new SolidColorBrush(
                Windows.UI.Color.FromArgb(bgAlpha, 25, 30, 40));
            OverlayBorder.BorderBrush = new SolidColorBrush(
                Windows.UI.Color.FromArgb(borderAlpha, 80, 200, 120));
            IconBackground.Fill = new SolidColorBrush(
                Windows.UI.Color.FromArgb(iconBgAlpha, 80, 200, 120));
        }
    }

    private void ShowOverlay()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Show();
    }

    private void HideOverlay()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Hide();
    }
}
