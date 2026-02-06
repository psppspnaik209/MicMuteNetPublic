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

    public MainViewModel(IAudioDeviceService audioService, ISettingsService settingsService)
    {
        _audioService = audioService;
        _settingsService = settingsService;

        // Subscribe to audio service events
        _audioService.MuteStateChanged += OnMuteStateChanged;
        _audioService.DevicesChanged += OnDevicesChanged;

        // Load initial state
        RefreshDevices();
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
            _audioService.SelectDevice(value.Id);
            IsMuted = _audioService.IsMuted;
            Volume = _audioService.Volume;

            // Save preference
            _settingsService.Settings.SelectedDeviceId = value.Id;
            _ = _settingsService.SaveAsync();
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
        _audioService.RefreshDevices();
        Devices = _audioService.Devices;

        // Restore selected device from settings or use default
        var savedDeviceId = _settingsService.Settings.SelectedDeviceId;
        SelectedDevice = Devices.FirstOrDefault(d => d.Id == savedDeviceId)
                      ?? Devices.FirstOrDefault(d => d.IsDefault)
                      ?? Devices.FirstOrDefault();
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
        await _settingsService.LoadAsync();
        
        VolumeControlEnabled = _settingsService.Settings.VolumeControlEnabled;
        MuteMode = _settingsService.Settings.MuteMode;
        
        RefreshDevices();
    }

    public void Dispose()
    {
        _audioService.MuteStateChanged -= OnMuteStateChanged;
        _audioService.DevicesChanged -= OnDevicesChanged;
    }
}
