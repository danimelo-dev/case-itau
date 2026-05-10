using Funds.Api.Models;
using Funds.Api.Persistence;
using Funds.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Funds.Api.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _context;

    public ClientRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(int idCliente, CancellationToken cancellationToken)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(x => x.IdCliente == idCliente, cancellationToken);
    }

    public Task UpdateAsync(Client client, CancellationToken cancellationToken)
    {
        _context.Clients.Update(client);
        return Task.CompletedTask;
    }
}