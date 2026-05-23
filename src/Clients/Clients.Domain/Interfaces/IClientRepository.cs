using Clients.Domain.Entities;

namespace Clients.Domain.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
