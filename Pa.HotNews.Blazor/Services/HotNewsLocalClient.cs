using Aneiang.Pa.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pa.HotNews.Blazor.Models;

namespace Pa.HotNews.Blazor.Services;

/// <summary>
/// Blazor Server 端直连 Aneiang.Pa 4.0 的新闻抓取服务（强类型）。
/// 使用 Aneiang.Pa.Pa.Source(name).GetAsync&lt;T&gt;() 替代旧的 INewsScraperFactory 模式。
/// </summary>
public sealed class HotNewsLocalClient
{
    private readonly IMemoryCache _cache;
    private readonly HotNewsCacheOptions _cacheOptions;

    public HotNewsLocalClient(
        IMemoryCache cache,
        IOptions<HotNewsCacheOptions> cacheOptions)
    {
        _cache = cache;
        _cacheOptions = cacheOptions.Value ?? new HotNewsCacheOptions();
    }

    /// <summary>
    /// 获取所有已注册的数据源名称（Recipe Name）。
    /// </summary>
    public Task<List<string>> GetSourcesAsync(CancellationToken ct = default)
    {
        _ = ct;

        var sources = Aneiang.Pa.Pa.Sources()
            .Select(r => r.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(sources);
    }

    /// <summary>
    /// 获取指定来源的新闻列表（支持缓存）。
    /// </summary>
    public async Task<ScrapeResult<NewsItem>?> GetNewsAsync(
        string source,
        bool bustCache = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;

        var key = source.Trim().ToLowerInvariant();
        if (key.Length == 0) return null;

        var cacheKey = $"hotnews:news:{key}";

        if (!bustCache && _cache.TryGetValue(cacheKey, out ScrapeResult<NewsItem>? cached) && cached is not null)
            return cached;

        ScrapeResult<NewsItem> result;
        try
        {
            var handle = Aneiang.Pa.Pa.Source(source);
            if (bustCache) handle = handle.NoCache();
            result = await handle.GetAsync<NewsItem>(ct);
        }
        catch (Exception ex)
        {
            result = ScrapeResult<NewsItem>.Fail(ex.Message);
        }

        // 成功/失败不同缓存时长：避免失败时长时间"坏缓存"
        var durationSeconds = result.IsSuccess
            ? _cacheOptions.DurationSeconds
            : _cacheOptions.FailureDurationSeconds;

        if (durationSeconds <= 0)
        {
            // <=0 表示不缓存
            return result;
        }

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(durationSeconds)
        });

        return result;
    }
}