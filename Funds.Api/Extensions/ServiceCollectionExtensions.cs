using Funds.Api.Cache;
using Funds.Api.Common;
using Funds.Api.Repositories;
using Funds.Api.Repositories.Interfaces;
using Funds.Api.Services;
using Funds.Api.Services.Interfaces;

namespace Funds.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationDependencies(
        this IServiceCollection services)
    {
        AddRepositories(services);
        AddServices(services);
        AddInfrastructureServices(services);

        return services;
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IClientRepository, ClientRepository>();

        services.AddScoped<IFundRepository, FundRepository>();

        services.AddScoped<IClientPositionRepository, ClientPositionRepository>();

        services.AddScoped<IOrderRepository, OrderRepository>();
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
    }

    private static void AddInfrastructureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<DistributedCacheService>();

        services.AddScoped<ICacheService>(provider =>
        {
            var distributedCacheService =
                provider.GetRequiredService<DistributedCacheService>();

            var logger =
                provider.GetRequiredService<ILogger<ResilientCacheService>>();

            return new ResilientCacheService(
                distributedCacheService,
                logger);
        });
    }
}