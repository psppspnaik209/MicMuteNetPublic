using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace MicMuteNet;

/// <summary>
/// Transparent overlay window that shows mute status.
/// Positioned at center-bottom of primary monitor.
/// </summary>
public sealed partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private bool _isMuted;
    private DispatcherTimer? _hideTimer;
    private double _opacity = 0.8;

    public OverlayWindow()
    {
        InitializeComponent();
        
        Title = "MicMuteNet Overlay";
        
        // Set up hide timer
        _hideTimer = new DispatcherTimer();
        _hideTimer.Interval = TimeSpan.FromSeconds(2);
        _hideTimer.Tick += (s, e) =>
        {
            _hideTimer.Stop();
            HideOverlay();
        };

        // Configure after content loads
        if (Content is FrameworkElement root)
        {
            root.Loaded += OnLoaded;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ConfigureOverlayWindow();
    }

    private void ConfigureOverlayWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Set size
        appWindow.Resize(new SizeInt32(280, 100));
        
        // Position at center-bottom of primary monitor
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var x = (displayArea.WorkArea.Width - 280) / 2;
        var y = displayArea.WorkArea.Height - 150; // 150px from bottom
        appWindow.Move(new PointInt32(x, y));

        // Make window click-through and always on top
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

        // Set always on top
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.SetBorderAndTitleBar(false, false);
        }
    }

    /// <summary>
    /// Sets the overlay opacity (0.0 to 1.0).
    /// </summary>
    public void SetOpacity(double opacity)
    {
        _opacity = Math.Clamp(opacity, 0.2, 1.0);
        UpdateVisuals();
    }

    public void ShowMuteStatus(bool isMuted, bool autoHide = true)
    {
        _isMuted = isMuted;

        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateVisuals();

            // Show window
            ShowOverlay();

            // Auto-hide after delay
            if (autoHide)
            {
                _hideTimer?.Stop();
                _hideTimer?.Start();
            }
        });
    }

    private void UpdateVisuals()
    {
        // Calculate alpha from opacity
        byte alpha = (byte)(_opacity * 255);
        byte backgroundAlpha = (byte)(_opacity * 204); // 80% of full opacity for background

        if (_isMuted)
        {
            // Muted - Red theme
            MuteIcon.Glyph = "\uEE56"; // Muted icon
            StatusText.Text = "MUTED";
            SubStatusText.Text = "Microphone is muted";
            
            OverlayBorder.Background = new SolidColorBrush(
                Windows.UI.Color.FromArgb(backgroundAlpha, 180, 50, 50));
            OverlayBorder.BorderBrush = new SolidColorBrush(
                Windows.UI.Color.FromArgb(alpha, 255, 100, 100));
            IconBackground.Fill = new SolidColorBrush(
                Windows.UI.Color.FromArgb((byte)(_opacity * 80), 255, 80, 80));
        }
        else
        {
            // Unmuted - Green theme
            MuteIcon.Glyph = "\uE720"; // Microphone icon
            StatusText.Text = "UNMUTED";
            SubStatusText.Text = "Microphone is active";
            
            OverlayBorder.Background = new SolidColorBrush(
                Windows.UI.Color.FromArgb(backgroundAlpha, 26, 26, 46));
            OverlayBorder.BorderBrush = new SolidColorBrush(
                Windows.UI.Color.FromArgb(alpha, 80, 200, 120));
            IconBackground.Fill = new SolidColorBrush(
                Windows.UI.Color.FromArgb((byte)(_opacity * 80), 80, 200, 120));
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
