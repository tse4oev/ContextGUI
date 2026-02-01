using System.Security.Principal;

namespace ContextGUI.Services;

/// <summary>
/// Provides administrator privilege checks.
/// </summary>
public static class AdminHelper
{
    /// <summary>
    /// Determines whether the current process is running with administrator privileges.
    /// </summary>
    /// <returns>True if administrator; otherwise false.</returns>
    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
