using Funds.Api.Models;

namespace Funds.Api.Repositories.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(int idCliente, CancellationToken cancellationToken);
    Task UpdateAsync(Client client, CancellationToken cancellationToken);
}