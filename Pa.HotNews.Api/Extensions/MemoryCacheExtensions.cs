using Microsoft.Extensions.Caching.Memory;

namespace Pa.HotNews.Api.Extensions;

/// <summary>
/// Async extension methods for <see cref="IMemoryCache"/>,
/// replacing the removed ICacheService from Aneiang.Pa 4.x.
/// </summary>
public static class MemoryCacheExtensions
{
    /// <summary>
    /// Gets a cached value by key, or creates and caches it using the async factory.
    /// Returns null when the factory returns null (cache miss = no entry stored).
    /// </summary>
    public static async Task<T?> GetOrCreateAsync<T>(
        this IMemoryCache cache,
        string key,
        Func<Task<T?>> factory,
        TimeSpan duration) where T : class
    {
        if (cache.TryGetValue(key, out T? cached))
            return cached;

        var result = await factory();
        if (result != null)
            cache.Set(key, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration });
        return result;
    }

    /// <summary>
    /// Removes the entry with the given key from the cache.
    /// </summary>
    public static Task RemoveAsync(this IMemoryCache cache, string key)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}