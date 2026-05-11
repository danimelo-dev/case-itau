using Polly;

namespace Funds.Api.Cache;

public class ResilientCacheService : ICacheService
{
    private readonly ICacheService _innerCacheService;
    private readonly ILogger<ResilientCacheService> _logger;

    public ResilientCacheService(
        ICacheService innerCacheService,
        ILogger<ResilientCacheService> logger)
    {
        _innerCacheService = innerCacheService;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken)
    {
        var policy = Policy<T?>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(100 * retryAttempt),
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Cache get failed. Retrying. Key: {CacheKey}, Attempt: {Attempt}, DelayMs: {DelayMs}",
                        key,
                        retryCount,
                        delay.TotalMilliseconds);
                });

        try
        {
            return await policy.ExecuteAsync(async () =>
                await _innerCacheService.GetAsync<T>(
                    key,
                    cancellationToken));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Cache get failed after retries. Falling back to database. Key: {CacheKey}",
                key);

            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(100 * retryAttempt),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Cache set failed. Retrying. Key: {CacheKey}, Attempt: {Attempt}, DelayMs: {DelayMs}",
                        key,
                        retryCount,
                        delay.TotalMilliseconds);
                });

        try
        {
            await policy.ExecuteAsync(async () =>
                await _innerCacheService.SetAsync(
                    key,
                    value,
                    expiration,
                    cancellationToken));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Cache set failed after retries. Continuing without cache. Key: {CacheKey}",
                key);
        }
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(100 * retryAttempt),
                onRetry: (exception, delay, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Cache remove failed. Retrying. Key: {CacheKey}, Attempt: {Attempt}, DelayMs: {DelayMs}",
                        key,
                        retryCount,
                        delay.TotalMilliseconds);
                });

        try
        {
            await policy.ExecuteAsync(async () =>
                await _innerCacheService.RemoveAsync(
                    key,
                    cancellationToken));
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Cache remove failed after retries. Continuing request. Key: {CacheKey}",
                key);
        }
    }
}