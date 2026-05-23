using Clients.Application.DTOs;

namespace Clients.Application.Interfaces;

public interface IClientCacheService
{
    Task<ClientResponseDto?> GetAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task SetAsync(ClientResponseDto client, CancellationToken cancellationToken = default);
    Task InvalidateAsync(Guid clientId, CancellationToken cancellationToken = default);
}
