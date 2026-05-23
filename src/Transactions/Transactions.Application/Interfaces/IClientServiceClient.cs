using Transactions.Application.DTOs;

namespace Transactions.Application.Interfaces;

public interface IClientServiceClient
{
    Task<bool> ClientExistsAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<ClientInfoDto?> GetClientInfoAsync(Guid clientId, CancellationToken cancellationToken = default);
}
