using Funds.Api.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Funds.Api.HealthChecks;

public class SqlServerHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public SqlServerHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("SQL Server is available.")
            : HealthCheckResult.Unhealthy("SQL Server is unavailable.");
    }
}