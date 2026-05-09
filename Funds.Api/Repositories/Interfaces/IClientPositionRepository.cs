using Funds.Api.Models;

namespace Funds.Api.Repositories.Interfaces;

public interface IClientPositionRepository
{
    Task<ClientPosition?> GetByClientAndFundAsync(int idCliente, int idFundo, CancellationToken cancellationToken);
    Task AddAsync(ClientPosition position, CancellationToken cancellationToken);
    Task UpdateAsync(ClientPosition position, CancellationToken cancellationToken);
}