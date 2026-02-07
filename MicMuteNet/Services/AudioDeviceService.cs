using MicMuteNet.Models;
using NAudio.CoreAudioApi;

namespace MicMuteNet.Services;

/// <summary>
/// Audio device service using NAudio's MMDevice API.
/// </summary>
public sealed class AudioDeviceService : IAudioDeviceService
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly List<MicrophoneDevice> _devices = [];
    private MMDevice? _selectedDevice;
    private string? _selectedDeviceId;
    private bool _disposed;

    public AudioDeviceService()
    {
        try
        {
            StartupLogger.Log("AudioDeviceService constructing...");
            _enumerator = new MMDeviceEnumerator();
            StartupLogger.Log("MMDeviceEnumerator created.");
            RefreshDevices();
            StartupLogger.Log($"AudioDeviceService constructed. Found {_devices.Count} devices.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"ERROR in AudioDeviceService constructor: {ex}");
            throw;
        }
    }

    public IReadOnlyList<MicrophoneDevice> Devices => _devices.AsReadOnly();

    public string? SelectedDeviceId => _selectedDeviceId;

    public bool IsMuted
    {
        get
        {
            try
            {
                return _selectedDevice?.AudioEndpointVolume?.Mute ?? false;
            }
            catch
            {
                return false;
            }
        }
    }

    public float Volume
    {
        get
        {
            try
            {
                return _selectedDevice?.AudioEndpointVolume?.MasterVolumeLevelScalar ?? 1.0f;
            }
            catch
            {
                return 1.0f;
            }
        }
    }

    public event EventHandler<bool>? MuteStateChanged;
    public event EventHandler? DevicesChanged;

    public void RefreshDevices()
    {
        _devices.Clear();

        try
        {
            var defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            var defaultDeviceId = defaultDevice?.ID;

            var endpoints = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var device in endpoints)
            {
                try
                {
                    _devices.Add(new MicrophoneDevice
                    {
                        Id = device.ID,
                        Name = device.FriendlyName,
                        IsDefault = device.ID == defaultDeviceId,
                        IsMuted = device.AudioEndpointVolume?.Mute ?? false,
                        Volume = device.AudioEndpointVolume?.MasterVolumeLevelScalar ?? 1.0f
                    });
                }
                catch
                {
                    // Skip devices that throw exceptions
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error enumerating devices: {ex.Message}");
        }

        DevicesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SelectDevice(string? deviceId)
    {
        try
        {
            _selectedDevice?.Dispose();
            _selectedDevice = null;

            if (string.IsNullOrEmpty(deviceId))
            {
                // Use default device
                _selectedDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            }
            else
            {
                _selectedDevice = _enumerator.GetDevice(deviceId);
            }

            _selectedDeviceId = _selectedDevice?.ID;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error selecting device: {ex.Message}");
            _selectedDevice = null;
            _selectedDeviceId = null;
        }
    }

    public bool SetMute(bool muted)
    {
        try
        {
            var endpoint = _selectedDevice?.AudioEndpointVolume;
            if (endpoint == null) return false;

            endpoint.Mute = muted;
            MuteStateChanged?.Invoke(this, muted);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting mute: {ex.Message}");
            return false;
        }
    }

    public bool ToggleMute()
    {
        var newState = !IsMuted;
        return SetMute(newState) ? newState : IsMuted;
    }

    public bool SetVolume(float volume)
    {
        try
        {
            var endpoint = _selectedDevice?.AudioEndpointVolume;
            if (endpoint == null) return false;

            endpoint.MasterVolumeLevelScalar = Math.Clamp(volume, 0f, 1f);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting volume: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _selectedDevice?.Dispose();
        _enumerator.Dispose();
    }
}
