using Funds.Api.Models;
using Funds.Api.Persistence;
using Funds.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Funds.Api.Repositories;

public class FundRepository : IFundRepository
{
    private readonly AppDbContext _context;

    public FundRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Fund?> GetByIdAsync(int idFundo, CancellationToken cancellationToken)
    {
        return await _context.Funds
            .FirstOrDefaultAsync(x => x.IdFundo == idFundo, cancellationToken);
    }
}