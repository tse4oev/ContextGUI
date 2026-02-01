namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Provides access to registry hives.
/// </summary>
public interface IRegistryWrapper
{
    /// <summary>
    /// Opens a subkey under HKEY_CLASSES_ROOT.
    /// </summary>
    IRegistryKey? OpenClassesRootSubKey(string name, bool writable = false);
}
