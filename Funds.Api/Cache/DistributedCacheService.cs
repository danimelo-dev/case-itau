using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Funds.Api.Cache;

public class DistributedCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(
        IDistributedCache cache,
        ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken)
    {
        var cachedValue = await _cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(cachedValue))
        {
            _logger.LogInformation("Cache miss. Key: {CacheKey}", key);
            return default;
        }

        _logger.LogInformation("Cache hit. Key: {CacheKey}", key);

        return JsonSerializer.Deserialize<T>(cachedValue, JsonOptions);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        var serializedValue = JsonSerializer.Serialize(value, JsonOptions);

        await _cache.SetStringAsync(
            key,
            serializedValue,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            },
            cancellationToken);

        _logger.LogInformation(
            "Cache set. Key: {CacheKey}, ExpirationSeconds: {ExpirationSeconds}",
            key,
            expiration.TotalSeconds);
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(key, cancellationToken);

        _logger.LogInformation("Cache removed. Key: {CacheKey}", key);
    }
}