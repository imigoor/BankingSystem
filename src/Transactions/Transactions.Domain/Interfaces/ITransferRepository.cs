using Transactions.Domain.Entities;

namespace Transactions.Domain.Interfaces;

public interface ITransferRepository
{
    Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transfer>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
