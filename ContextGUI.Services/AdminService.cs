using ContextGUI.Services.Interfaces;

namespace ContextGUI.Services;

/// <summary>
/// Default admin privilege checker.
/// </summary>
public sealed class AdminService : IAdminService
{
    /// <inheritdoc />
    public bool IsAdministrator() => AdminHelper.IsAdministrator();
}
