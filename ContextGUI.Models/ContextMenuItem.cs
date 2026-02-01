using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ContextGUI.Models;

public sealed class ContextMenuItem : INotifyPropertyChanged
{
    private string _displayName = string.Empty;
    private bool _isEnabled = true;
    private string? _iconPath;
    private bool _isSystemItem;
    private string _category = string.Empty;
    private string? _command;
    private bool _isLegacyHandler;
    private bool _isModernHandler;

    public string Name { get; init; } = string.Empty;
    public string RegistryPath { get; init; } = string.Empty;

    public string DisplayName
    {
        get => _displayName;
        set => SetField(ref _displayName, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public string? IconPath
    {
        get => _iconPath;
        set => SetField(ref _iconPath, value);
    }

    public bool IsSystemItem
    {
        get => _isSystemItem;
        set => SetField(ref _isSystemItem, value);
    }

    public string Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }

    public string? Command
    {
        get => _command;
        set => SetField(ref _command, value);
    }

    public bool IsLegacyHandler
    {
        get => _isLegacyHandler;
        set => SetField(ref _isLegacyHandler, value);
    }

    public bool IsModernHandler
    {
        get => _isModernHandler;
        set => SetField(ref _isModernHandler, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}
