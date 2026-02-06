using System.Runtime.InteropServices;
using MicMuteNet.Models;
using Windows.System;

namespace MicMuteNet.Services;

/// <summary>
/// Global hotkey service using Win32 RegisterHotKey API.
/// </summary>
public sealed partial class HotkeyService : IHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 9000;

    private IntPtr _hwnd;
    private bool _isRegistered;
    private HotkeyConfiguration? _currentHotkey;
    private readonly object _lock = new();

    // Win32 P/Invoke
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr CreateWindowExW(
        uint dwExStyle,
        [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
        [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
        uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyWindow(IntPtr hWnd);

    // Modifier key flags
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;

    public event EventHandler? HotkeyPressed;

    public HotkeyConfiguration? CurrentHotkey => _currentHotkey;
    public bool IsRegistered => _isRegistered;

    public HotkeyService()
    {
        // Create a message-only window for receiving hotkey messages
        _hwnd = IntPtr.Zero;
    }

    public bool RegisterHotkey(HotkeyConfiguration config)
    {
        lock (_lock)
        {
            if (config.IsEmpty) return false;

            // Unregister existing hotkey first
            if (_isRegistered)
            {
                UnregisterHotkey();
            }

            // Get window handle from current dispatcher
            var hwnd = GetMessageWindowHandle();
            if (hwnd == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Failed to get message window handle");
                return false;
            }

            _hwnd = hwnd;

            // Build modifiers
            uint modifiers = MOD_NOREPEAT;
            if (config.Alt) modifiers |= MOD_ALT;
            if (config.Control) modifiers |= MOD_CONTROL;
            if (config.Shift) modifiers |= MOD_SHIFT;
            if (config.Win) modifiers |= MOD_WIN;

            // Get virtual key code
            uint vk = (uint)config.Key;

            // Register the hotkey
            bool result = RegisterHotKey(_hwnd, HOTKEY_ID, modifiers, vk);
            if (result)
            {
                _isRegistered = true;
                _currentHotkey = config;
                System.Diagnostics.Debug.WriteLine($"Hotkey registered: {config}");
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"Failed to register hotkey: {error}");
            }

            return result;
        }
    }

    public void UnregisterHotkey()
    {
        lock (_lock)
        {
            if (_isRegistered && _hwnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hwnd, HOTKEY_ID);
                _isRegistered = false;
                _currentHotkey = null;
                System.Diagnostics.Debug.WriteLine("Hotkey unregistered");
            }
        }
    }

    private IntPtr GetMessageWindowHandle()
    {
        // For WinUI 3, we need to use the main window's handle
        // This will be set from MainWindow
        return _hwnd;
    }

    public void SetWindowHandle(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public void ProcessHotkeyMessage()
    {
        HotkeyPressed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        UnregisterHotkey();
    }
}
