using Funds.Api.Models;
using Funds.Api.Persistence;
using Funds.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Funds.Api.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public async Task<List<Order>> GetAllAsync(int? idCliente, CancellationToken cancellationToken)
    {
        var query = _context.Orders.AsQueryable();

        if (idCliente.HasValue)
        {
            query = query.Where(x => x.IdCliente == idCliente.Value);
        }

        return await query
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync(cancellationToken);
    }
}