namespace ContextGUI.Models;

public sealed class RegistryResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public string? BackupPath { get; init; }
}
