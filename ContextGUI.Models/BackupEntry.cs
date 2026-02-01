namespace ContextGUI.Models;

public sealed class BackupEntry
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? SourceHint { get; init; }
}
