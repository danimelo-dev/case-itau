using Funds.Api.Enums;
using Funds.Api.Models;

namespace Funds.Api.Persistence.Seed;

public static class DatabaseSeed
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (context.Clients.Any())
            return;

        var client = new Client
        {
            Nome = "Daniel Melo",
            Cpf = "12345678901",
            SaldoDisponivel = 100000,
            CriadoEm = DateTime.UtcNow
        };

        var fund = new Fund
        {
            Nome = "Fundo Alpha",
            HorarioCorte = new TimeSpan(14, 0, 0),
            ValorCota = 100,
            ValorMinimoAporte = 1000,
            ValorMinimoPermanencia = 500,
            StatusCaptacao = FundStatus.Aberto,
            CriadoEm = DateTime.UtcNow
        };

        context.Clients.Add(client);
        context.Funds.Add(fund);

        await context.SaveChangesAsync();

        var position = new ClientPosition
        {
            IdCliente = client.IdCliente,
            IdFundo = fund.IdFundo,
            QuantidadeCotas = 10,
            CriadoEm = DateTime.UtcNow
        };

        context.ClientPositions.Add(position);

        await context.SaveChangesAsync();
    }
}