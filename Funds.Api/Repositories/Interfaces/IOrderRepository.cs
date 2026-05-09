using Funds.Api.Models;

namespace Funds.Api.Repositories.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<List<Order>> GetAllAsync(int? idCliente, CancellationToken cancellationToken);
}