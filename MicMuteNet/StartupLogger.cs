namespace MicMuteNet;

internal static class StartupLogger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "MicMuteNet.startup.log");
    private static bool _loggingEnabled = true; // Enabled by default, controlled by settings

    public static void SetLoggingEnabled(bool enabled)
    {
        _loggingEnabled = enabled;
        // Delete log file when logging is disabled
        if (!enabled && File.Exists(LogPath))
        {
            try { File.Delete(LogPath); } catch { }
        }
    }

    public static void Log(string message)
    {
        if (!_loggingEnabled) return;
        
        try
        {
            var line = $"{DateTimeOffset.Now:O} {message}";
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
        }
    }
}
