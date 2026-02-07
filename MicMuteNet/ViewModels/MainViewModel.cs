using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MicMuteNet.Models;
using MicMuteNet.Services;

namespace MicMuteNet.ViewModels;

/// <summary>
/// ViewModel for the main window.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IAudioDeviceService _audioService;
    private readonly ISettingsService _settingsService;
    private bool _isInitializing = true; // Initialize to true to block saves during construction

    public MainViewModel(IAudioDeviceService audioService, ISettingsService settingsService)
    {
        _audioService = audioService;
        _settingsService = settingsService;

        // Subscribe to audio service events
        _audioService.MuteStateChanged += OnMuteStateChanged;
        _audioService.DevicesChanged += OnDevicesChanged;

        // Don't call RefreshDevices() here - it will be called in InitializeAsync() after settings are loaded
    }

    [ObservableProperty]
    private IReadOnlyList<MicrophoneDevice> _devices = [];

    [ObservableProperty]
    private MicrophoneDevice? _selectedDevice;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private float _volume = 1.0f;

    [ObservableProperty]
    private bool _volumeControlEnabled;

    [ObservableProperty]
    private MuteMode _muteMode = MuteMode.Toggle;

    /// <summary>
    /// Gets the mute status text.
    /// </summary>
    public string MuteStatusText => IsMuted ? "Muted" : "Unmuted";

    /// <summary>
    /// Gets the volume percentage display.
    /// </summary>
    public string VolumePercentage => $"{Volume * 100:F0}%";

    partial void OnSelectedDeviceChanged(MicrophoneDevice? value)
    {
        if (value != null)
        {
            StartupLogger.Log($"Device selected: {value.Name} (ID: {value.Id})");
            
            _audioService.SelectDevice(value.Id);
            IsMuted = _audioService.IsMuted;
            Volume = _audioService.Volume;

            // Save only if initialization is complete
            if (!_isInitializing)
            {
                StartupLogger.Log($"Saving device to settings: {value.Id}");
                _settingsService.Settings.SelectedDeviceId = value.Id;
                _ = _settingsService.SaveAsync();
            }
            else
            {
                StartupLogger.Log("Initialization in progress, not saving");
            }
        }
        else
        {
            StartupLogger.Log("Device selection cleared");
        }
    }

    partial void OnIsMutedChanged(bool value)
    {
        OnPropertyChanged(nameof(MuteStatusText));
    }

    partial void OnVolumeChanged(float value)
    {
        OnPropertyChanged(nameof(VolumePercentage));
    }

    partial void OnMuteModeChanged(MuteMode value)
    {
        _settingsService.Settings.MuteMode = value;
        _ = _settingsService.SaveAsync();
    }

    partial void OnVolumeControlEnabledChanged(bool value)
    {
        _settingsService.Settings.VolumeControlEnabled = value;
        _ = _settingsService.SaveAsync();
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = _audioService.ToggleMute();
    }

    [RelayCommand]
    private void SetMuted(bool muted)
    {
        _audioService.SetMute(muted);
        IsMuted = muted;
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        StartupLogger.Log("RefreshDevices called");
        _audioService.RefreshDevices();
        Devices = _audioService.Devices;

        // Try to restore saved device, but don't force a selection
        var savedDeviceId = _settingsService.Settings.SelectedDeviceId;
        if (!string.IsNullOrEmpty(savedDeviceId))
        {
            var savedDevice = Devices.FirstOrDefault(d => d.Id == savedDeviceId);
            if (savedDevice != null)
            {
                StartupLogger.Log($"Restoring saved device: {savedDevice.Name}");
                SelectedDevice = savedDevice;
            }
            else
            {
                StartupLogger.Log($"Saved device not found: {savedDeviceId}");
                // Don't select anything - let user choose
            }
        }
        else
        {
            StartupLogger.Log("No saved device, user must select one");
        }
    }

    [RelayCommand]
    private void SetVolume(float volume)
    {
        if (_audioService.SetVolume(volume))
        {
            Volume = volume;
        }
    }

    private void OnMuteStateChanged(object? sender, bool muted)
    {
        IsMuted = muted;
    }

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        Devices = _audioService.Devices;
    }

    public async Task InitializeAsync()
    {
        // _isInitializing is already true from constructor
        
        await _settingsService.LoadAsync();
        
        VolumeControlEnabled = _settingsService.Settings.VolumeControlEnabled;
        MuteMode = _settingsService.Settings.MuteMode;
        
        RefreshDevices();
        
        // Now allow saves
        _isInitializing = false;
        StartupLogger.Log("Initialization complete, saves now enabled");
    }

    public void Dispose()
    {
        _audioService.MuteStateChanged -= OnMuteStateChanged;
        _audioService.DevicesChanged -= OnDevicesChanged;
    }
}
