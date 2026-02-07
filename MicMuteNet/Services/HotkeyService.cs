using System.Diagnostics;
using System.Runtime.InteropServices;
using MicMuteNet.Models;
using Windows.System;

namespace MicMuteNet.Services;

/// <summary>
/// Global hotkey service using low-level keyboard hook.
/// Uses WH_KEYBOARD_LL which is safe and won't trigger anti-cheats.
/// </summary>
public sealed class HotkeyService : IHotkeyService
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    // Virtual key codes for modifiers
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;    // Alt key
    private const int VK_SHIFT = 0x10;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc;
    private HotkeyConfiguration? _currentHotkey;
    private bool _isKeyDown;
    private readonly object _lock = new();

    public event EventHandler? HotkeyPressed;

    public HotkeyConfiguration? CurrentHotkey => _currentHotkey;
    public bool IsRegistered => _hookId != IntPtr.Zero && _currentHotkey != null && !_currentHotkey.IsEmpty;

    public HotkeyService()
    {
        try
        {
            StartupLogger.Log("HotkeyService constructing...");
            // Install the low-level keyboard hook
            _hookProc = HookCallback;
            _hookId = SetHook(_hookProc);
            StartupLogger.Log($"Keyboard hook installed: {_hookId != IntPtr.Zero}");
            Debug.WriteLine($"Keyboard hook installed: {_hookId != IntPtr.Zero}");
            StartupLogger.Log("HotkeyService constructed successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"ERROR in HotkeyService constructor: {ex}");
            throw;
        }
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        if (module?.ModuleName == null) return IntPtr.Zero;
        
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _currentHotkey != null && !_currentHotkey.IsEmpty)
        {
            var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var vkCode = (int)hookStruct.vkCode;
            var isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
            var isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

            // Check if this is our registered hotkey
            if (vkCode == (int)_currentHotkey.Key)
            {
                bool modifiersMatch = true;

                if (!_currentHotkey.IgnoreModifiers)
                {
                    // Check modifier keys
                    bool ctrlDown = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                    bool altDown = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
                    bool shiftDown = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                    bool winDown = (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;

                    modifiersMatch = (ctrlDown == _currentHotkey.Control) &&
                                     (altDown == _currentHotkey.Alt) &&
                                     (shiftDown == _currentHotkey.Shift) &&
                                     (winDown == _currentHotkey.Win);
                }

                if (modifiersMatch)
                {
                    if (isKeyDown && !_isKeyDown)
                    {
                        _isKeyDown = true;
                        Debug.WriteLine($"Hotkey pressed: {_currentHotkey}");
                        
                        // Fire the event
                        HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    }
                    else if (isKeyUp)
                    {
                        _isKeyDown = false;
                    }

                    // Suppress the key if configured
                    if (_currentHotkey.Suppress)
                    {
                        return (IntPtr)1; // Suppress the key
                    }
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public bool RegisterHotkey(HotkeyConfiguration config)
    {
        lock (_lock)
        {
            if (config.IsEmpty)
            {
                _currentHotkey = null;
                return false;
            }

            _currentHotkey = config;
            _isKeyDown = false;
            Debug.WriteLine($"Hotkey registered: {config}");
            return true;
        }
    }

    public void UnregisterHotkey()
    {
        lock (_lock)
        {
            _currentHotkey = null;
            _isKeyDown = false;
            Debug.WriteLine("Hotkey unregistered");
        }
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            Debug.WriteLine("Keyboard hook removed");
        }
        _hookProc = null;
    }
}
