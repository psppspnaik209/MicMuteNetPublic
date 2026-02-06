using System.Media;
using NAudio.Wave;

namespace MicMuteNet.Services;

/// <summary>
/// Audio notification service using NAudio for WAV playback.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly string _mutedSoundPath;
    private readonly string _unmutedSoundPath;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _audioFile;
    private readonly object _lock = new();

    public bool Enabled { get; set; } = true;
    public float Volume { get; set; } = 0.5f;

    public NotificationService()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds");
        _mutedSoundPath = Path.Combine(basePath, "beep300.wav");    // Lower beep for muted
        _unmutedSoundPath = Path.Combine(basePath, "beep750.wav");  // Higher beep for unmuted
    }

    public void PlayMutedSound()
    {
        PlaySound(_mutedSoundPath);
    }

    public void PlayUnmutedSound()
    {
        PlaySound(_unmutedSoundPath);
    }

    private void PlaySound(string filePath)
    {
        if (!Enabled) return;
        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine($"Sound file not found: {filePath}");
            return;
        }

        Task.Run(() =>
        {
            lock (_lock)
            {
                try
                {
                    // Clean up previous playback
                    _waveOut?.Stop();
                    _waveOut?.Dispose();
                    _audioFile?.Dispose();

                    // Load and play sound
                    _audioFile = new AudioFileReader(filePath);
                    _audioFile.Volume = Volume;
                    
                    _waveOut = new WaveOutEvent();
                    _waveOut.Init(_audioFile);
                    _waveOut.Play();

                    // Wait for playback to complete
                    while (_waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(50);
                    }

                    // Clean up
                    _waveOut.Dispose();
                    _audioFile.Dispose();
                    _waveOut = null;
                    _audioFile = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
                }
            }
        });
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioFile?.Dispose();
        }
    }
}
