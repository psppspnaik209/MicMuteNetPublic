using System.Threading;

namespace MicMuteNet.Helpers;

/// <summary>
/// Ensures only one instance of the application can run at a time.
/// </summary>
public sealed class SingleInstanceHelper : IDisposable
{
    private readonly Mutex _mutex;
    private bool _hasHandle;

    public SingleInstanceHelper(string appGuid)
    {
        _mutex = new Mutex(true, $"Global\\{appGuid}", out _hasHandle);
    }

    public bool IsFirstInstance => _hasHandle;

    public void Dispose()
    {
        if (_hasHandle)
        {
            _mutex.ReleaseMutex();
            _hasHandle = false;
        }
        _mutex.Dispose();
    }
}
