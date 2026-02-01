using Microsoft.Win32;

namespace ContextGUI.Services.Interfaces;

/// <summary>
/// Abstraction over a registry key to enable testability.
/// </summary>
public interface IRegistryKey : IDisposable
{
    /// <summary>
    /// Gets the full name of the key.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns all subkey names.
    /// </summary>
    string[] GetSubKeyNames();

    /// <summary>
    /// Opens a subkey by name.
    /// </summary>
    IRegistryKey? OpenSubKey(string name, bool writable = false);

    /// <summary>
    /// Reads a value by name.
    /// </summary>
    object? GetValue(string name);

    /// <summary>
    /// Sets a registry value with the specified type.
    /// </summary>
    void SetValue(string name, object value, RegistryValueKind valueKind);

    /// <summary>
    /// Deletes a registry value by name.
    /// </summary>
    /// <param name="name">Value name.</param>
    /// <param name="throwOnMissingValue">True to throw if missing.</param>
    void DeleteValue(string name, bool throwOnMissingValue);

    /// <summary>
    /// Deletes a subkey tree by name.
    /// </summary>
    /// <param name="subkey">Subkey name.</param>
    /// <param name="throwOnMissingSubKey">True to throw if missing.</param>
    void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey);

    /// <summary>
    /// Creates or opens a subkey.
    /// </summary>
    /// <param name="subkey">Subkey name.</param>
    /// <returns>Opened subkey.</returns>
    IRegistryKey CreateSubKey(string subkey);
}
