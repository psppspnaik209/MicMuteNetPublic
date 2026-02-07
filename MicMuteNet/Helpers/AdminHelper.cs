using System.Runtime.InteropServices;
using System.Security.Principal;

namespace MicMuteNet.Helpers;

/// <summary>
/// Helper for admin privileges and Task Scheduler integration.
/// </summary>
public static class AdminHelper
{
    /// <summary>
    /// Checks if the current process is running with administrator privileges.
    /// </summary>
    public static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
