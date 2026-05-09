using Funds.Api.Models;

namespace Funds.Api.Repositories.Interfaces;

public interface IFundRepository
{
    Task<Fund?> GetByIdAsync(int idFundo, CancellationToken cancellationToken);
}