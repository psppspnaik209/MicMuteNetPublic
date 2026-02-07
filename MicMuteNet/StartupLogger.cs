namespace MicMuteNet;

internal static class StartupLogger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "MicMuteNet.startup.log");

    public static void Log(string message)
    {
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
