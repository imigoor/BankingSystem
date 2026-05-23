using Microsoft.EntityFrameworkCore;
using Transactions.Domain.Entities;
using Transactions.Domain.Interfaces;

namespace Transactions.Infrastructure.Persistence.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly TransactionsDbContext _context;

    public TransferRepository(TransactionsDbContext context)
    {
        _context = context;
    }

    public async Task<Transfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Transfers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IEnumerable<Transfer>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Transfers
            .Where(t => t.SenderUserId == userId || t.ReceiverUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default)
        => await _context.Transfers.AddAsync(transfer, cancellationToken);

    public Task UpdateAsync(Transfer transfer, CancellationToken cancellationToken = default)
    {
        _context.Transfers.Update(transfer);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
