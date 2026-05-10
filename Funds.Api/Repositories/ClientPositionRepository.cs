using Funds.Api.Models;
using Funds.Api.Persistence;
using Funds.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Funds.Api.Repositories;

public class ClientPositionRepository : IClientPositionRepository
{
    private readonly AppDbContext _context;

    public ClientPositionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ClientPosition?> GetByClientAndFundAsync(
        int idCliente,
        int idFundo,
        CancellationToken cancellationToken)
    {
        return await _context.ClientPositions
            .FirstOrDefaultAsync(
                x => x.IdCliente == idCliente && x.IdFundo == idFundo,
                cancellationToken);
    }

    public async Task AddAsync(ClientPosition position, CancellationToken cancellationToken)
    {
        await _context.ClientPositions.AddAsync(position, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ClientPosition position, CancellationToken cancellationToken)
    {
        _context.ClientPositions.Update(position);

        await _context.SaveChangesAsync(cancellationToken);
    }
}