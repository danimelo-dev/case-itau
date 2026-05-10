using Funds.Api.Repositories;
using Funds.Api.Repositories.Interfaces;

namespace Funds.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IFundRepository, FundRepository>();
        services.AddScoped<IClientPositionRepository, ClientPositionRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}