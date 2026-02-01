namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Provides administrator privilege checks.
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Determines whether the current process is running with administrator privileges.
    /// </summary>
    /// <returns>True if administrator; otherwise false.</returns>
    bool IsAdministrator();
}
