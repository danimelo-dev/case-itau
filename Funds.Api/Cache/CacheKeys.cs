namespace Funds.Api.Cache;

public static class CacheKeys
{
    public static string Orders(int? idCliente)
    {
        return idCliente.HasValue
            ? $"orders:client:{idCliente.Value}"
            : "orders:all";
    }
}