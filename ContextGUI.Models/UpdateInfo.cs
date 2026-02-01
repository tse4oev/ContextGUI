using System;

namespace ContextGUI.Models;

public sealed class UpdateInfo
{
    public Version Version { get; init; } = new Version(0, 0, 0, 0);
    public string? ReleaseUrl { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}
