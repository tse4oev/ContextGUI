using CommunityToolkit.Mvvm.ComponentModel;
using ContextGUI.Models;

namespace ContextGUI.Dialogs;

public partial class EditItemDialogViewModel : ObservableObject
{
    public EditItemDialogViewModel(ContextMenuItem item)
    {
        DisplayName = item.DisplayName;
        IconPath = item.IconPath;
        Command = item.Command;
        IsLegacyHandler = item.IsLegacyHandler;
        IsModernHandler = item.IsModernHandler;
        IsEditable = !(IsLegacyHandler || IsModernHandler);
    }

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string? _iconPath;

    [ObservableProperty]
    private string? _command;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool IsLegacyHandler { get; }

    public bool IsModernHandler { get; }

    public bool IsEditable { get; }

    public bool Validate()
    {
        if (IsLegacyHandler)
        {
            ErrorMessage = "Этот элемент относится к legacy-обработчику и не поддерживает редактирование.";
            return false;
        }

        if (IsModernHandler)
        {
            ErrorMessage = "Этот элемент относится к современному обработчику и не поддерживает редактирование.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ErrorMessage = "Название не может быть пустым.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Command))
        {
            ErrorMessage = "Команда не может быть пустой.";
            return false;
        }

        ErrorMessage = string.Empty;
        return true;
    }

    public ContextMenuItemEditModel ToEditModel()
    {
        return new ContextMenuItemEditModel
        {
            DisplayName = DisplayName.Trim(),
            IconPath = string.IsNullOrWhiteSpace(IconPath) ? null : IconPath.Trim(),
            Command = string.IsNullOrWhiteSpace(Command) ? null : Command.Trim()
        };
    }
}
