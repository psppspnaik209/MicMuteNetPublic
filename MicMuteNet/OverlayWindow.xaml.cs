using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace MicMuteNet;

/// <summary>
/// Transparent overlay window that shows mute status.
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

    public OverlayWindow()
    {
        InitializeComponent();
        
        // Configure window
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

        // Set size and position
        appWindow.Resize(new SizeInt32(200, 80));
        
        // Position in top-right corner
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var x = displayArea.WorkArea.Width - 220;
        var y = 20;
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

    public void ShowMuteStatus(bool isMuted, bool autoHide = true)
    {
        _isMuted = isMuted;

        DispatcherQueue.TryEnqueue(() =>
        {
            // Update visuals
            if (isMuted)
            {
                MuteIcon.Glyph = "\uEE56"; // Muted icon
                StatusText.Text = "Muted";
                OverlayBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(204, 200, 50, 50)); // Red tint
            }
            else
            {
                MuteIcon.Glyph = "\uE720"; // Microphone icon
                StatusText.Text = "Unmuted";
                OverlayBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(204, 50, 150, 50)); // Green tint
            }

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
