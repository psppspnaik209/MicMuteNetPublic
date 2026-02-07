using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using WinRT;
using MicMuteNet.Helpers;

namespace MicMuteNet;

public static class Program
{
    private const string AppGuid = "A7B8C9D0-1234-5678-9ABC-DEF012345678";
    private static SingleInstanceHelper? _singleInstance;

    [STAThread]
    public static void Main(string[] args)
    {
        // Single instance check
        _singleInstance = new SingleInstanceHelper(AppGuid);
        if (!_singleInstance.IsFirstInstance)
        {
            StartupLogger.Log("Another instance is already running. Exiting.");
            
            // Show notification to user
            System.Windows.Forms.MessageBox.Show(
                "MicMuteNet is already running!\n\nCheck the system tray for the microphone icon.",
                "MicMuteNet - Already Running",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            
            _singleInstance.Dispose();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            StartupLogger.Log($"Unhandled exception: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            StartupLogger.Log($"Unobserved task exception: {e.Exception}");
            e.SetObserved();
        };

        StartupLogger.Log("Starting MicMuteNet...");

        var needsBootstrap = !IsPackaged();
        if (needsBootstrap)
        {
            try
            {
                StartupLogger.Log("Initializing Windows App SDK bootstrap...");
                Bootstrap.Initialize(0x00010008);
                StartupLogger.Log("Windows App SDK bootstrap initialized.");
            }
            catch (Exception ex)
            {
                StartupLogger.Log($"Bootstrap initialization failed: {ex}");
                throw;
            }
        }

        try
        {
            StartupLogger.Log("Initializing COM wrappers...");
            ComWrappersSupport.InitializeComWrappers();
            StartupLogger.Log("COM wrappers initialized.");
            
            StartupLogger.Log("Starting Application.Start...");
            Application.Start((p) =>
            {
                StartupLogger.Log("Application.Start callback entered.");
                try
                {
                    var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                    System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                    StartupLogger.Log("SynchronizationContext set.");
                }
                catch (Exception ex)
                {
                    StartupLogger.Log($"SynchronizationContext setup failed: {ex}");
                    throw;
                }
                
                try
                {
                    StartupLogger.Log("Creating App instance...");
                    _ = new App();
                    StartupLogger.Log("App instance created.");
                }
                catch (Exception ex)
                {
                    StartupLogger.Log($"App construction failed: {ex}");
                    throw;
                }
            });
            StartupLogger.Log("Application.Start returned.");
        }
        catch (Exception ex)
        {
            StartupLogger.Log($"Fatal exception: {ex}");
            throw;
        }
        finally
        {
            if (needsBootstrap)
            {
                Bootstrap.Shutdown();
            }
            StartupLogger.Log("MicMuteNet shutdown complete.");
        }
    }

    private static bool IsPackaged()
    {
        try
        {
            _ = Windows.ApplicationModel.Package.Current;
            return true;
        }
        catch
        {
            return false;
        }
    }

}
