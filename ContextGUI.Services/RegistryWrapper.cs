using Microsoft.Win32;
using ContextGUI.Services.Interfaces;

namespace ContextGUI.Services;

/// <summary>
/// Real registry wrapper for production use.
/// </summary>
public sealed class RegistryWrapper : IRegistryWrapper
{
    /// <inheritdoc />
    public IRegistryKey? OpenClassesRootSubKey(string name, bool writable = false)
    {
        var key = Registry.ClassesRoot.OpenSubKey(name, writable);
        return key == null ? null : new RegistryKeyAdapter(key);
    }

    private sealed class RegistryKeyAdapter : IRegistryKey
    {
        private readonly RegistryKey _key;

        public RegistryKeyAdapter(RegistryKey key)
        {
            _key = key;
        }

        public string Name => _key.Name;

        public string[] GetSubKeyNames() => _key.GetSubKeyNames();

        public IRegistryKey? OpenSubKey(string name, bool writable = false)
        {
            var subKey = _key.OpenSubKey(name, writable);
            return subKey == null ? null : new RegistryKeyAdapter(subKey);
        }

        public object? GetValue(string name) => _key.GetValue(name);

        public void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            _key.SetValue(name, value, valueKind);
        }

        public void DeleteValue(string name, bool throwOnMissingValue)
        {
            _key.DeleteValue(name, throwOnMissingValue);
        }

        public void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey)
        {
            _key.DeleteSubKeyTree(subkey, throwOnMissingSubKey);
        }

        public IRegistryKey CreateSubKey(string subkey)
        {
            var created = _key.CreateSubKey(subkey);
            if (created == null)
            {
                throw new InvalidOperationException($"Failed to create subkey: {subkey}");
            }

            return new RegistryKeyAdapter(created);
        }

        public void Dispose()
        {
            _key.Dispose();
        }
    }
}
