namespace ContextGUI.Models;

public sealed class ContextMenuItemEditModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public string? Command { get; set; }
}
