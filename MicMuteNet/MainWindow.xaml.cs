using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MicMuteNet.Models;
using MicMuteNet.Services;
using MicMuteNet.ViewModels;
using H.NotifyIcon;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace MicMuteNet;

/// <summary>
/// Main application window.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IHotkeyService _hotkeyService;
    private readonly ISettingsService _settingsService;
    private TaskbarIcon? _trayIcon;
    private bool _isExiting;
    private bool _isCapturingHotkey;
    private HotkeyConfiguration _pendingHotkey = new();

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        _hotkeyService = App.Services.GetRequiredService<IHotkeyService>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        
        InitializeComponent();

        // Set window title and size
        Title = "MicMuteNet";
        SetWindowSize(420, 620);

        // Subscribe to ViewModel changes
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Subscribe to hotkey events
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        // Handle window close to minimize to tray
        AppWindow.Closing += AppWindow_Closing;

        // Initialize UI when content is loaded
        if (Content is FrameworkElement rootElement)
        {
            rootElement.Loaded += MainWindow_Loaded;
        }
    }

    private void SetWindowSize(int width, int height)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(width, height));
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();

        // Set window handle for hotkey service
        var hwnd = WindowNative.GetWindowHandle(this);
        if (_hotkeyService is HotkeyService hs)
        {
            hs.SetWindowHandle(hwnd);
        }

        // Initialize system tray
        InitializeTrayIcon();

        // Populate devices
        RefreshDeviceList();

        // Set initial mute mode
        SetMuteModeSelection(_viewModel.MuteMode);

        // Set volume control state
        VolumeControlCheckBox.IsChecked = _viewModel.VolumeControlEnabled;
        VolumeSliderPanel.Visibility = _viewModel.VolumeControlEnabled
            ? Visibility.Visible : Visibility.Collapsed;

        // Load and register hotkey from settings
        LoadHotkeyFromSettings();

        UpdateMuteUI();
    }

    private void LoadHotkeyFromSettings()
    {
        var settings = _settingsService.Settings;
        if (settings.Hotkey != null && !settings.Hotkey.IsEmpty)
        {
            _pendingHotkey = settings.Hotkey;
            HotkeyTextBox.Text = settings.Hotkey.ToString();
            SuppressHotkeyCheckBox.IsChecked = settings.Hotkey.Suppress;
            IgnoreModifiersCheckBox.IsChecked = settings.Hotkey.IgnoreModifiers;
            
            // Register the hotkey
            _hotkeyService.RegisterHotkey(settings.Hotkey);
        }
    }

    private void InitializeTrayIcon()
    {
        var iconPath = _viewModel.IsMuted
            ? Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "microphoneDisabled.ico")
            : Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "microphoneEnabled.ico");

        try
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = _viewModel.IsMuted ? "MicMuteNet - Muted" : "MicMuteNet - Unmuted",
                Icon = new System.Drawing.Icon(iconPath)
            };

            // Context menu
            var contextMenu = new MenuFlyout();

            var toggleItem = new MenuFlyoutItem { Text = "Toggle Mute" };
            toggleItem.Click += (s, e) => _viewModel.ToggleMuteCommand.Execute(null);
            contextMenu.Items.Add(toggleItem);

            contextMenu.Items.Add(new MenuFlyoutSeparator());

            var showItem = new MenuFlyoutItem { Text = "Show" };
            showItem.Click += (s, e) => ShowWindow();
            contextMenu.Items.Add(showItem);

            var exitItem = new MenuFlyoutItem { Text = "Exit" };
            exitItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextFlyout = contextMenu;

            // Single left click to show window
            _trayIcon.LeftClickCommand = new RelayCommand(ShowWindow);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create tray icon: {ex.Message}");
        }
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null) return;

        var iconPath = _viewModel.IsMuted
            ? Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "microphoneDisabled.ico")
            : Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "microphoneEnabled.ico");

        try
        {
            _trayIcon.Icon = new System.Drawing.Icon(iconPath);
            _trayIcon.ToolTipText = _viewModel.IsMuted ? "MicMuteNet - Muted" : "MicMuteNet - Unmuted";
        }
        catch { /* Icon may not exist */ }
    }

    private void ShowWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Show();
        Activate();
    }

    private void HideWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Hide();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _hotkeyService.Dispose();
        _trayIcon?.Dispose();
        Close();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (!_isExiting)
        {
            // Minimize to tray instead of closing
            args.Cancel = true;
            HideWindow();
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _viewModel.ToggleMuteCommand.Execute(null);
        });
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.IsMuted):
                    UpdateMuteUI();
                    UpdateTrayIcon();
                    break;
                case nameof(MainViewModel.Devices):
                    RefreshDeviceList();
                    break;
                case nameof(MainViewModel.Volume):
                    VolumeSlider.Value = _viewModel.Volume * 100;
                    VolumeText.Text = _viewModel.VolumePercentage;
                    break;
            }
        });
    }

    private void UpdateMuteUI()
    {
        MuteToggleButton.IsChecked = _viewModel.IsMuted;
        MuteStatusText.Text = _viewModel.MuteStatusText;

        // Update icon: E720 = Microphone, EE56 = MicrophoneOff (muted)
        MuteIcon.Glyph = _viewModel.IsMuted ? "\uEE56" : "\uE720";

        // Update description
        MuteDescriptionText.Text = _viewModel.IsMuted
            ? "Microphone is muted"
            : "Microphone is active";
    }

    private void RefreshDeviceList()
    {
        DeviceComboBox.ItemsSource = _viewModel.Devices;
        DeviceComboBox.SelectedItem = _viewModel.SelectedDevice;
    }

    private void SetMuteModeSelection(MuteMode mode)
    {
        var index = mode switch
        {
            MuteMode.Toggle => 0,
            MuteMode.PushToTalk => 1,
            MuteMode.PushToMute => 2,
            _ => 0
        };
        MuteModeRadios.SelectedIndex = index;
    }

    private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceComboBox.SelectedItem is MicrophoneDevice device)
        {
            _viewModel.SelectedDevice = device;
            UpdateMuteUI();
        }
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.RefreshDevicesCommand.Execute(null);
        StatusText.Text = "Devices refreshed";
    }

    private void MuteToggle_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleMuteCommand.Execute(null);
    }

    private void VolumeControl_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || VolumeSliderPanel == null) return;

        var enabled = VolumeControlCheckBox.IsChecked == true;
        _viewModel.VolumeControlEnabled = enabled;
        VolumeSliderPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_viewModel == null || VolumeText == null) return;

        var volume = (float)(e.NewValue / 100.0);
        _viewModel.SetVolumeCommand.Execute(volume);
        VolumeText.Text = $"{e.NewValue:F0}%";
    }

    private void MuteMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null) return;

        if (MuteModeRadios.SelectedItem is RadioButton radio && radio.Tag is string tag)
        {
            _viewModel.MuteMode = tag switch
            {
                "Toggle" => MuteMode.Toggle,
                "PushToTalk" => MuteMode.PushToTalk,
                "PushToMute" => MuteMode.PushToMute,
                _ => MuteMode.Toggle
            };
        }
    }

    // Hotkey capture handlers
    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = true;
        HotkeyTextBox.Text = "Press a key...";
        StatusText.Text = "Press a key combination to set as hotkey";
    }

    private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = false;
        if (_pendingHotkey.IsEmpty)
        {
            HotkeyTextBox.Text = "";
        }
        StatusText.Text = "Ready";
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_isCapturingHotkey) return;

        e.Handled = true;

        // Ignore modifier-only presses
        if (e.Key == VirtualKey.Control || e.Key == VirtualKey.Menu || 
            e.Key == VirtualKey.Shift || e.Key == VirtualKey.LeftWindows ||
            e.Key == VirtualKey.RightWindows)
        {
            return;
        }

        // Build hotkey configuration
        var config = new HotkeyConfiguration
        {
            Key = e.Key,
            Control = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down),
            Alt = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down),
            Shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down),
            Win = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.LeftWindows).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down) ||
                  Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.RightWindows).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down),
            Suppress = SuppressHotkeyCheckBox.IsChecked == true,
            IgnoreModifiers = IgnoreModifiersCheckBox.IsChecked == true
        };

        _pendingHotkey = config;
        HotkeyTextBox.Text = config.ToString();

        // Register the hotkey
        if (_hotkeyService.RegisterHotkey(config))
        {
            StatusText.Text = $"Hotkey registered: {config}";
            
            // Save to settings
            _settingsService.Settings.Hotkey = config;
            _ = _settingsService.SaveAsync();
        }
        else
        {
            StatusText.Text = "Failed to register hotkey (may be in use)";
        }

        // Remove focus
        _isCapturingHotkey = false;
    }

    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        _hotkeyService.UnregisterHotkey();
        _pendingHotkey = new HotkeyConfiguration();
        HotkeyTextBox.Text = "";
        
        // Save to settings
        _settingsService.Settings.Hotkey = new HotkeyConfiguration();
        _ = _settingsService.SaveAsync();
        
        StatusText.Text = "Hotkey cleared";
    }

    private void HotkeyOptions_Changed(object sender, RoutedEventArgs e)
    {
        if (_pendingHotkey.IsEmpty) return;

        _pendingHotkey.Suppress = SuppressHotkeyCheckBox.IsChecked == true;
        _pendingHotkey.IgnoreModifiers = IgnoreModifiersCheckBox.IsChecked == true;

        // Re-register with new options
        _hotkeyService.RegisterHotkey(_pendingHotkey);

        // Save to settings
        _settingsService.Settings.Hotkey = _pendingHotkey;
        _ = _settingsService.SaveAsync();
    }
}
