using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MicMuteNet.Helpers;
using MicMuteNet.Models;
using MicMuteNet.Services;
using MicMuteNet.ViewModels;
using H.NotifyIcon;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace MicMuteNet;

/// <summary>
/// Main application window.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IHotkeyService _hotkeyService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private OverlayWindow? _overlayWindow;
    private TaskbarIcon? _trayIcon;
    private bool _isExiting;
    private bool _isCapturingHotkey;
    private HotkeyConfiguration _pendingHotkey = new();
    private bool _isLoadingSettings = true; // Block saves during initialization

    public MainWindow(MainViewModel viewModel)
    {
        try
        {
            StartupLogger.Log("MainWindow constructing...");
            _viewModel = viewModel;
            
            StartupLogger.Log("Getting services from DI...");
            _hotkeyService = App.Services.GetRequiredService<IHotkeyService>();
            _settingsService = App.Services.GetRequiredService<ISettingsService>();
            _notificationService = App.Services.GetRequiredService<INotificationService>();
            StartupLogger.Log("Services obtained.");
            
            StartupLogger.Log("Calling InitializeComponent()...");
            InitializeComponent();
            StartupLogger.Log("InitializeComponent() completed.");

            // Set window title and size
            Title = "MicMuteNet";
            SetWindowSize(560, 620);

            // Subscribe to ViewModel changes
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Subscribe to hotkey events
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
            _hotkeyService.HotkeyReleased += OnHotkeyReleased;

            // Handle window close to minimize to tray
            AppWindow.Closing += AppWindow_Closing;

            // Initialize UI when content is loaded
            if (Content is FrameworkElement rootElement)
            {
                rootElement.Loaded += MainWindow_Loaded;
                rootElement.KeyDown += RootElement_KeyDown;
            }

            StartupLogger.Log("MainWindow constructed successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"FATAL ERROR in MainWindow constructor: {ex}");
            throw;
        }
    }

    private void SetWindowSize(int width, int height)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(width, height));
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
            System.Diagnostics.Debug.WriteLine($"Failed to set window icon: {ex.Message}");
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            StartupLogger.Log("MainWindow_Loaded starting...");
            
            StartupLogger.Log("Calling _viewModel.InitializeAsync()...");
            await _viewModel.InitializeAsync();
            StartupLogger.Log("ViewModel initialized.");

            // Create overlay window (must be after app is fully initialized)
            StartupLogger.Log("Creating OverlayWindow...");
            _overlayWindow = new OverlayWindow();
            StartupLogger.Log("OverlayWindow created.");

            // Set window and taskbar icon
            StartupLogger.Log("Setting window icon...");
            SetWindowIcon();

            // Initialize system tray
            StartupLogger.Log("Initializing tray icon...");
            InitializeTrayIcon();
            StartupLogger.Log("Tray icon initialized.");

            // Populate devices
            StartupLogger.Log("Refreshing device list...");
            RefreshDeviceList();

            // Set initial mute mode
            SetMuteModeSelection(_viewModel.MuteMode);

            // Set volume control state
            VolumeControlCheckBox.IsChecked = _viewModel.VolumeControlEnabled;
            VolumeSliderPanel.Visibility = _viewModel.VolumeControlEnabled
                ? Visibility.Visible : Visibility.Collapsed;

            // Load and register hotkey from settings
            StartupLogger.Log("Loading hotkey settings...");
            LoadHotkeyFromSettings();

            // Load settings to UI
            LoadSettingsToUI();

            // Enable saves now that initialization is complete
            _isLoadingSettings = false;

            UpdateMuteUI();

            // Apply default mute on startup (after all config is loaded)
            if (_settingsService.Settings.DefaultMuteOnStartup && _viewModel.SelectedDevice != null)
            {
                StartupLogger.Log("Applying default mute on startup...");
                _viewModel.SetMutedCommand.Execute(true);
                UpdateMuteUI();
                StartupLogger.Log("Default mute applied.");
            }

            if (_settingsService.Settings.StartMinimized)
            {
                HideWindow();
            }
            
            StartupLogger.Log("MainWindow_Loaded completed successfully.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"ERROR in MainWindow_Loaded: {ex}");
            System.Diagnostics.Debug.WriteLine($"Initialization failed: {ex}");
            try
            {
                if (StatusText != null)
                {
                    StatusText.Text = $"Initialization failed: {ex.Message}";
                }
            }
            catch
            {
                // StatusText might not be available yet
            }
        }
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
        var iconPath = IconHelper.GetIconPath(_viewModel.IsMuted);

        try
        {
            if (!IconHelper.IconExists(iconPath))
            {
                System.Diagnostics.Debug.WriteLine($"Tray icon not found: {iconPath}");
                StatusText.Text = $"Icon not found: {iconPath}";
                return;
            }

            _trayIcon = new TaskbarIcon
            {
                ToolTipText = _viewModel.IsMuted ? "MicMuteNet - Muted" : "MicMuteNet - Unmuted",
                Icon = new System.Drawing.Icon(iconPath)
            };

            ConfigureTrayMenuBehavior();

            // Force the tray icon to be created immediately
            _trayIcon.ForceCreate();

            // IMPORTANT: Delay menu creation to ensure tray icon is fully initialized
            // This prevents the menu from being cut off on first launch
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                System.Threading.Thread.Sleep(100); // Small delay for tray icon initialization
                CreateTrayContextMenu();
            });
            
            System.Diagnostics.Debug.WriteLine("Tray icon created successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create tray icon: {ex.Message}");
            StatusText.Text = $"Tray error: {ex.Message}";
        }
    }

    private void CreateTrayContextMenu()
    {
        if (_trayIcon == null) return;

        try
        {
            // Create context menu with WinUI MenuFlyout
            var contextMenu = new MenuFlyout();
            contextMenu.Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft;
            
            if (Content is FrameworkElement rootElement)
            {
                contextMenu.XamlRoot = rootElement.XamlRoot;
            }

            var toggleItem = new MenuFlyoutItem { Text = "Toggle Mute" };
            toggleItem.Click += TrayToggle_Click;
            contextMenu.Items.Add(toggleItem);

            contextMenu.Items.Add(new MenuFlyoutSeparator());

            var showItem = new MenuFlyoutItem { Text = "Show Window" };
            showItem.Click += TrayShow_Click;
            contextMenu.Items.Add(showItem);

            var exitItem = new MenuFlyoutItem { Text = "Exit" };
            exitItem.Click += TrayExit_Click;
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextFlyout = contextMenu;

            // Double click to show window
            _trayIcon.LeftClickCommand = new RelayCommand(() => InvokeOnUI(ShowWindow));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create context menu: {ex.Message}");
        }
    }

    private void TrayToggle_Click(object sender, RoutedEventArgs e)
    {
        InvokeOnUI(() => _viewModel.ToggleMuteCommand.Execute(null));
    }

    private void ConfigureTrayMenuBehavior()
    {
        if (_trayIcon == null)
        {
            return;
        }

        TrySetEnumProperty(_trayIcon, "MenuActivation", "LeftOrRightClick");
        TrySetEnumProperty(_trayIcon, "ContextMenuMode", "SecondWindow");
    }

    private static void TrySetEnumProperty(object target, string propertyName, string enumValue)
    {
        var property = target.GetType().GetProperty(propertyName);
        if (property?.PropertyType.IsEnum != true || !property.CanWrite)
        {
            return;
        }

        try
        {
            var value = Enum.Parse(property.PropertyType, enumValue);
            property.SetValue(target, value);
        }
        catch
        {
        }
    }

    private void TrayShow_Click(object sender, RoutedEventArgs e)
    {
        InvokeOnUI(ShowWindow);
    }

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        InvokeOnUI(ExitApplication);
    }

    private void OpenConfig_Click(object sender, RoutedEventArgs e)
    {
        var settingsPath = _settingsService.SettingsPath;
        if (!File.Exists(settingsPath))
        {
            _ = _settingsService.SaveAsync();
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = settingsPath,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to open settings: {ex.Message}";
        }
    }

    private async void OpenAbout_Click(object sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionText = version != null ? $"Version {version.Major}.{version.Minor}.{version.Build}" : "Version 1.0.0";

        var dialog = new ContentDialog
        {
            Title = "About MicMuteNet",
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
            Content = CreateAboutContent(versionText)
        };

        await dialog.ShowAsync();
    }

    private StackPanel CreateAboutContent(string versionText)
    {
        var content = new StackPanel { Spacing = 16, MaxWidth = 450 };

        // App name and version
        var titleStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Spacing = 8 };
        titleStack.Children.Add(new TextBlock
        {
            Text = "MicMuteNet",
            Style = (Style)Application.Current.Resources["TitleTextBlockStyle"],
            HorizontalAlignment = HorizontalAlignment.Center
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = versionText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        });
        content.Children.Add(titleStack);

        // Description
        content.Children.Add(new TextBlock
        {
            Text = "A lightweight microphone mute utility with global hotkey support, system tray integration, and on-screen overlay notifications.",
            TextWrapping = TextWrapping.Wrap,
            HorizontalTextAlignment = TextAlignment.Center,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        });

        // Made with love
        var loveText = new TextBlock
        {
            Text = "Made with Love by TNBB",
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
        };
        loveText.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        content.Children.Add(loveText);

        // GitHub link
        var githubButton = new HyperlinkButton
        {
            Content = "View on GitHub",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        githubButton.Click += (s, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/psppspnaik209/MicMuteNetPublic",
                    UseShellExecute = true
                });
            }
            catch { }
        };
        content.Children.Add(githubButton);

        // License link
        var licenseButton = new HyperlinkButton
        {
            Content = "MIT License",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        licenseButton.Click += (s, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://opensource.org/licenses/MIT",
                    UseShellExecute = true
                });
            }
            catch { }
        };
        content.Children.Add(licenseButton);

        // Donate button
        var donateButton = new Button
        {
            Content = "Buy Me a Coffee",
            HorizontalAlignment = HorizontalAlignment.Center,
            Style = (Style)Application.Current.Resources["AccentButtonStyle"]
        };
        donateButton.Click += (s, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://buymeacoffee.com/psppspnaik209",
                    UseShellExecute = true
                });
            }
            catch { }
        };
        content.Children.Add(donateButton);

        // License text
        var licenseBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Child = new TextBlock
            {
                Text = "MIT License\n\nPermission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files, to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            }
        };
        content.Children.Add(licenseBorder);

        return content;
    }

    private void InvokeOnUI(Action action)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        _ = DispatcherQueue.TryEnqueue(() => action());
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon == null) return;

        var iconPath = IconHelper.GetIconPath(_viewModel.IsMuted);

        try
        {
            _trayIcon.Icon = new System.Drawing.Icon(iconPath);
            _trayIcon.ToolTipText = _viewModel.IsMuted ? "MicMuteNet - Muted" : "MicMuteNet - Unmuted";
        }
        catch { /* Icon may not exist */ }
    }

    private void ShowWindow()
    {
        WindowExtensions.Show(this);
        Activate();
    }

    private void HideWindow()
    {
        WindowExtensions.Hide(this);
    }

    private void ExitApplication()
    {
        _isExiting = true;
        
        // Dispose resources
        _viewModel?.Dispose();
        _hotkeyService?.Dispose();
        _trayIcon?.Dispose();
        _overlayWindow?.Close();
        
        // Close the window
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
            switch (_viewModel.MuteMode)
            {
                case MuteMode.Toggle:
                    // Toggle mode: press to toggle mute state
                    _viewModel.ToggleMuteCommand.Execute(null);
                    break;
                    
                case MuteMode.PushToTalk:
                    // Push to talk: press to unmute
                    _viewModel.SetMutedCommand.Execute(false);
                    break;
                    
                case MuteMode.PushToMute:
                    // Push to mute: press to mute
                    _viewModel.SetMutedCommand.Execute(true);
                    break;
            }
        });
    }

    private void OnHotkeyReleased(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (_viewModel.MuteMode)
            {
                case MuteMode.Toggle:
                    // Toggle mode: do nothing on release
                    break;
                    
                case MuteMode.PushToTalk:
                    // Push to talk: release to mute (return to muted state)
                    _viewModel.SetMutedCommand.Execute(true);
                    break;
                    
                case MuteMode.PushToMute:
                    // Push to mute: release to unmute (return to unmuted state)
                    _viewModel.SetMutedCommand.Execute(false);
                    break;
            }
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
                    PlayNotificationSound();
                    ShowOverlay();
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

        // Update icon: E720 = Microphone, E74F = MicOff (better mute icon)
        MuteIcon.Glyph = _viewModel.IsMuted ? "\uE74F" : "\uE720";

        // Update description
        MuteDescriptionText.Text = _viewModel.IsMuted
            ? "Microphone is muted"
            : "Microphone is active";
    }

    private void PlayNotificationSound()
    {
        if (_settingsService.Settings.NotificationEnabled)
        {
            _notificationService.Volume = _settingsService.Settings.NotificationVolume;
            if (_viewModel.IsMuted)
                _notificationService.PlayMutedSound();
            else
                _notificationService.PlayUnmutedSound();
        }
    }

    private void ShowOverlay()
    {
        if (_settingsService.Settings.OverlayEnabled && _overlayWindow != null)
        {
            _overlayWindow.ShowMuteStatus(_viewModel.IsMuted, _settingsService.Settings.OverlayDuration);
        }
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
    private void RecordHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (_isCapturingHotkey)
        {
            // Stop recording
            StopRecording();
        }
        else
        {
            // Start recording
            StartRecording();
        }
    }

    private void StartRecording()
    {
        _isCapturingHotkey = true;
        RecordingIndicator.Visibility = Visibility.Visible;
        RecordButtonText.Text = "Stop";
        RecordButtonIcon.Glyph = "\uE71A"; // Stop icon
        StatusText.Text = "Press any key combination...";
        
        // Temporarily unregister current hotkey so we can capture it
        _hotkeyService.UnregisterHotkey();
        
        // Subscribe to keyboard hook for capture
        if (_hotkeyService is HotkeyService hs)
        {
            hs.HotkeyPressed -= OnHotkeyPressed; // Temporarily disable toggle
        }
        
        // Focus the window to capture keys
        Activate();
    }

    private void StopRecording()
    {
        _isCapturingHotkey = false;
        RecordingIndicator.Visibility = Visibility.Collapsed;
        RecordButtonText.Text = "Record";
        RecordButtonIcon.Glyph = "\uE7C8"; // Record icon
        StatusText.Text = "Ready";
        
        // Re-subscribe to hotkey
        if (_hotkeyService is HotkeyService hs)
        {
            hs.HotkeyPressed += OnHotkeyPressed;
        }
        
        // Re-register the hotkey if we have one
        if (!_pendingHotkey.IsEmpty)
        {
            _hotkeyService.RegisterHotkey(_pendingHotkey);
        }
    }

    private void RootElement_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (!_isCapturingHotkey)
        {
            return;
        }

        e.Handled = true;

        // Escape cancels recording
        if (e.Key == VirtualKey.Escape)
        {
            StopRecording();
            return;
        }

        // Ignore modifier-only presses (wait for the actual key)
        if (e.Key == VirtualKey.Control || e.Key == VirtualKey.Menu || 
            e.Key == VirtualKey.Shift || e.Key == VirtualKey.LeftWindows ||
            e.Key == VirtualKey.RightWindows ||
            e.Key == VirtualKey.LeftControl || e.Key == VirtualKey.RightControl ||
            e.Key == VirtualKey.LeftShift || e.Key == VirtualKey.RightShift ||
            e.Key == VirtualKey.LeftMenu || e.Key == VirtualKey.RightMenu)
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

        // Stop recording and register the hotkey
        StopRecording();
        
        if (_hotkeyService.RegisterHotkey(config))
        {
            StatusText.Text = $"Hotkey set: {config}";
            
            // Save to settings
            _settingsService.Settings.Hotkey = config;
            _ = _settingsService.SaveAsync();
        }
        else
        {
            StatusText.Text = "Failed to register hotkey";
        }
    }

    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        // Stop recording if active
        if (_isCapturingHotkey)
        {
            StopRecording();
        }
        
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

    // Settings event handlers
    private void NotificationEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        _settingsService.Settings.NotificationEnabled = NotificationEnabledCheckBox.IsChecked == true;
        _ = _settingsService.SaveAsync();
    }

    private void OverlayEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        _settingsService.Settings.OverlayEnabled = OverlayEnabledCheckBox.IsChecked == true;
        _ = _settingsService.SaveAsync();
    }

    private void StartWithWindows_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        
        var enable = StartWithWindowsCheckBox.IsChecked == true;
        _settingsService.Settings.RunAtStartup = enable;
        SetStartWithWindows(enable);
        _ = _settingsService.SaveAsync();
    }

    private void StartMinimized_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        _settingsService.Settings.StartMinimized = StartMinimizedCheckBox.IsChecked == true;
        _ = _settingsService.SaveAsync();
    }

    private void DefaultMuteOnStartup_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        _settingsService.Settings.DefaultMuteOnStartup = DefaultMuteOnStartupCheckBox.IsChecked == true;
        _ = _settingsService.SaveAsync();
    }

    private void CollectLogs_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        var enabled = CollectLogsCheckBox.IsChecked == true;
        _settingsService.Settings.CollectLogs = enabled;
        StartupLogger.SetLoggingEnabled(enabled);
        _ = _settingsService.SaveAsync();
    }

    private void SetStartWithWindows(bool enable)
    {
        try
        {
            var isAdmin = MicMuteNet.Helpers.AdminHelper.IsRunningAsAdministrator();
            var exePath = Environment.ProcessPath ?? "";
            
            if (string.IsNullOrEmpty(exePath))
                return;

            const string appName = "MicMuteNet";

            if (isAdmin)
            {
                // Use Task Scheduler for admin mode
                SetStartWithWindowsTaskScheduler(enable, exePath, appName);
            }
            else
            {
                // Use registry for normal mode
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key == null) return;
                
                if (enable)
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                    StatusText.Text = "Startup enabled (Registry)";
                }
                else
                {
                    key.DeleteValue(appName, false);
                    StatusText.Text = "Startup disabled";
                }
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to set startup: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Startup error: {ex}");
        }
    }

    private void SetStartWithWindowsTaskScheduler(bool enable, string exePath, string taskName)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "schtasks.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (enable)
            {
                // Create scheduled task
                psi.Arguments = $"/Create /SC ONLOGON /TN \"{taskName}\" /TR \"\\\"{exePath}\\\"\" /RL HIGHEST /F";
                var process = System.Diagnostics.Process.Start(psi);
                process?.WaitForExit();
                
                if (process?.ExitCode == 0)
                {
                    StatusText.Text = "Startup enabled (Task Scheduler - Admin Mode)";
                }
                else
                {
                    StatusText.Text = "Failed to create startup task";
                }
            }
            else
            {
                // Delete scheduled task
                psi.Arguments = $"/Delete /TN \"{taskName}\" /F";
                var process = System.Diagnostics.Process.Start(psi);
                process?.WaitForExit();
                StatusText.Text = "Startup disabled";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Task Scheduler error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Task Scheduler error: {ex}");
        }
    }

    private void LoadSettingsToUI()
    {
        var settings = _settingsService.Settings;
        
        // Notification settings
        NotificationEnabledCheckBox.IsChecked = settings.NotificationEnabled;
        SoundVolumeSlider.Value = settings.NotificationVolume * 100;
        SoundVolumeText.Text = $"{(int)(settings.NotificationVolume * 100)}%";
        
        // Load output devices
        if (_notificationService is NotificationService ns)
        {
            var devices = ns.GetOutputDevices();
            OutputDeviceComboBox.ItemsSource = devices;
            
            // Select the saved device or default
            var selectedDevice = devices.FirstOrDefault(d => d.DeviceNumber == settings.OutputDeviceNumber)
                                 ?? devices.FirstOrDefault();
            OutputDeviceComboBox.SelectedItem = selectedDevice;
        }
        
        // Overlay settings
        OverlayEnabledCheckBox.IsChecked = settings.OverlayEnabled;
        OverlayOpacitySlider.Value = settings.OverlayOpacity * 100;
        OverlayOpacityText.Text = $"{(int)(settings.OverlayOpacity * 100)}%";
        OverlayDurationTextBox.Text = settings.OverlayDuration.ToString("F1");
        _overlayWindow?.SetOpacity(settings.OverlayOpacity);
        
        // Startup settings
        StartWithWindowsCheckBox.IsChecked = settings.RunAtStartup;
        StartMinimizedCheckBox.IsChecked = settings.StartMinimized;
        DefaultMuteOnStartupCheckBox.IsChecked = settings.DefaultMuteOnStartup;
    }

    private void SoundVolume_Changed(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isLoadingSettings) return;
        var volume = (float)(e.NewValue / 100.0);
        if (SoundVolumeText != null)
            SoundVolumeText.Text = $"{(int)e.NewValue}%";
        
        _settingsService.Settings.NotificationVolume = volume;
        if (_notificationService is NotificationService ns)
        {
            ns.Volume = volume;
        }
        _ = _settingsService.SaveAsync();
    }

    private void OutputDevice_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings) return;
        if (OutputDeviceComboBox.SelectedItem is OutputDevice device)
        {
            _settingsService.Settings.OutputDeviceNumber = device.DeviceNumber;
            if (_notificationService is NotificationService ns)
            {
                ns.OutputDeviceNumber = device.DeviceNumber;
            }
            _ = _settingsService.SaveAsync();
        }
    }

    private void OverlayOpacity_Changed(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isLoadingSettings) return;
        var opacity = e.NewValue / 100.0;
        if (OverlayOpacityText != null)
            OverlayOpacityText.Text = $"{(int)e.NewValue}%";
        
        _settingsService.Settings.OverlayOpacity = opacity;
        _overlayWindow?.SetOpacity(opacity);
        _ = _settingsService.SaveAsync();
    }

    private void OverlayDuration_Changed(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
    {
        if (_isLoadingSettings) return;
        
        // Allow both spinner and text input
        if (!double.IsNaN(args.NewValue) && args.NewValue >= 0.1 && args.NewValue <= 5.0)
        {
            var duration = args.NewValue;
            _settingsService.Settings.OverlayDuration = duration;
            _ = _settingsService.SaveAsync();
        }
        else if (double.IsNaN(args.NewValue))
        {
            // User cleared the box or entered invalid text - reset to current value
            sender.Value = _settingsService.Settings.OverlayDuration;
        }
    }

    private void OverlayDurationTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        ValidateAndSaveOverlayDuration();
    }

    private void OverlayDurationTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ValidateAndSaveOverlayDuration();
            e.Handled = true;
        }
    }

    private void ValidateAndSaveOverlayDuration()
    {
        if (double.TryParse(OverlayDurationTextBox.Text, out var duration))
        {
            duration = Math.Clamp(duration, 0.1, 5.0);
            OverlayDurationTextBox.Text = duration.ToString("F1");
            _settingsService.Settings.OverlayDuration = duration;
            _ = _settingsService.SaveAsync();
        }
        else
        {
            // Invalid input - reset to current value
            OverlayDurationTextBox.Text = _settingsService.Settings.OverlayDuration.ToString("F1");
        }
    }

    private void DurationUp_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        var current = _settingsService.Settings.OverlayDuration;
        var newValue = Math.Clamp(current + 0.1, 0.1, 5.0);
        OverlayDurationTextBox.Text = newValue.ToString("F1");
        _settingsService.Settings.OverlayDuration = newValue;
        _ = _settingsService.SaveAsync();
    }

    private void DurationDown_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings) return;
        var current = _settingsService.Settings.OverlayDuration;
        var newValue = Math.Clamp(current - 0.1, 0.1, 5.0);
        OverlayDurationTextBox.Text = newValue.ToString("F1");
        _settingsService.Settings.OverlayDuration = newValue;
        _ = _settingsService.SaveAsync();
    }
}
