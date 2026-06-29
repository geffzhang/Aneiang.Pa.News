using Aneiang.Pa;
using Aneiang.Pa.Abstractions;
using ModelContextProtocol.Server;

namespace Pa.HotNews.Api.Mcp;

/// <summary>
/// MCP Tools that expose Aneiang.Pa 4.0 scraper capabilities to AI agents.
/// </summary>
[McpServerToolType]
public class PaMcpTools
{
    /// <summary>
    /// List all registered data sources (recipes) available for scraping.
    /// </summary>
    [McpServerTool(Name = "list_pa_sources")]
    public static SourceInfo[] ListSources()
    {
        return Aneiang.Pa.Pa.Sources()
            .Select(r => new SourceInfo(
                Name: r.Name,
                DisplayName: r.DisplayName ?? r.Name,
                Category: r.Category ?? string.Empty))
            .ToArray();
    }

    /// <summary>
    /// Scrape hot news / trending data from a specified source.
    /// </summary>
    /// <param name="source">Source name, e.g. "WeiBo", "ZhiHu", "BaiDu", "DouYin", "TouTiao", "BiliBili"</param>
    /// <param name="noCache">If true, bypass cache and fetch fresh data.</param>
    [McpServerTool(Name = "scrape_pa_source")]
    public static async Task<ScrapeToolResult> ScrapeAsync(
        string source,
        bool noCache = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var handle = Aneiang.Pa.Pa.Source(source);
            if (noCache) handle = handle.NoCache();

            var result = await handle.GetAsync(cancellationToken);

            return new ScrapeToolResult(
                Success: result.IsSuccess,
                Source: source,
                Error: result.ErrorMessage,
                UpdatedTime: result.UpdatedTime,
                Count: result.Data?.Count ?? 0,
                Items: result.Data?
                    .Select(d => new Dictionary<string, string?>(d))
                    .ToArray()
            );
        }
        catch (Exception ex)
        {
            return new ScrapeToolResult(
                Success: false,
                Source: source,
                Error: ex.Message,
                UpdatedTime: null,
                Count: 0,
                Items: Array.Empty<Dictionary<string, string?>>()
            );
        }
    }

    /// <summary>
    /// Simple health check for the Pa scraper infrastructure.
    /// </summary>
    [McpServerTool(Name = "pa_health_check")]
    public static HealthInfo HealthCheck()
    {
        var sources = Aneiang.Pa.Pa.Sources();
        return new HealthInfo(
            Status: "healthy",
            SourceCount: sources.Count,
            Sources: sources.Select(r => r.Name).ToArray()
        );
    }
}

// ---- DTOs ----

/// <summary>Data source info DTO for MCP serialization.</summary>
public sealed record SourceInfo(string Name, string DisplayName, string Category);

/// <summary>Scrape result DTO for MCP serialization.</summary>
public sealed record ScrapeToolResult(
    bool Success,
    string Source,
    string? Error,
    DateTimeOffset? UpdatedTime,
    int Count,
    IReadOnlyList<IReadOnlyDictionary<string, string?>>? Items);

/// <summary>Health check DTO for MCP serialization.</summary>
public sealed record HealthInfo(string Status, int SourceCount, string[] Sources);