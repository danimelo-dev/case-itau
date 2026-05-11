using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Funds.Api.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return _connectionMultiplexer.IsConnected
            ? Task.FromResult(HealthCheckResult.Healthy("Redis is available."))
            : Task.FromResult(HealthCheckResult.Unhealthy("Redis is unavailable."));
    }
}