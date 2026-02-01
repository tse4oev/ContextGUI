namespace ContextGUI.Models;

public sealed class UpdateSettings
{
    public bool AutoCheckUpdates { get; set; } = true;
    public string? SkipVersion { get; set; }
    public string? RepositoryOwner { get; set; }
    public string? RepositoryName { get; set; }
}
