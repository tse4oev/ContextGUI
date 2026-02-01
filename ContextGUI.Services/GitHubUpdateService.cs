using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ContextGUI.Models;
using ContextGUI.Services.Interfaces;

namespace ContextGUI.Services;

public sealed class GitHubUpdateService : IUpdateService
{
    private readonly ILoggingService _logger;
    private readonly HttpClient _httpClient;

    public GitHubUpdateService(ILoggingService logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ContextGUI");
    }

    public Version GetCurrentVersion()
    {
        var entry = System.Reflection.Assembly.GetEntryAssembly();
        return entry?.GetName().Version
               ?? typeof(GitHubUpdateService).Assembly.GetName().Version
               ?? new Version(0, 0, 0, 0);
    }

    public async Task<RegistryResult<UpdateInfo>> CheckForUpdateAsync(UpdateSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings == null)
        {
            return new RegistryResult<UpdateInfo>
            {
                Success = false,
                Error = "Update settings not provided."
            };
        }

        if (string.IsNullOrWhiteSpace(settings.RepositoryOwner) || string.IsNullOrWhiteSpace(settings.RepositoryName))
        {
            return new RegistryResult<UpdateInfo>
            {
                Success = false,
                Error = "Repository is not configured."
            };
        }

        var url = $"https://api.github.com/repos/{settings.RepositoryOwner}/{settings.RepositoryName}/releases/latest";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new RegistryResult<UpdateInfo>
                {
                    Success = false,
                    Error = $"GitHub API error: {(int)response.StatusCode}"
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
            var htmlUrl = root.GetProperty("html_url").GetString();
            var body = root.TryGetProperty("body", out var bodyElement) ? bodyElement.GetString() : null;
            var publishedAt = root.TryGetProperty("published_at", out var publishedElement)
                ? publishedElement.GetDateTimeOffset()
                : (DateTimeOffset?)null;

            if (!TryParseVersion(tag, out var version))
            {
                return new RegistryResult<UpdateInfo>
                {
                    Success = false,
                    Error = $"Invalid release tag: {tag}"
                };
            }

            return new RegistryResult<UpdateInfo>
            {
                Success = true,
                Value = new UpdateInfo
                {
                    Version = version,
                    ReleaseUrl = htmlUrl,
                    Notes = body,
                    PublishedAt = publishedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check updates");
            return new RegistryResult<UpdateInfo>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static bool TryParseVersion(string tag, out Version version)
    {
        tag = tag.Trim();
        if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            tag = tag[1..];
        }

        version = new Version(0, 0, 0, 0);
        if (!Version.TryParse(tag, out var parsed) || parsed == null)
        {
            return false;
        }

        version = parsed;
        return true;
    }
}
