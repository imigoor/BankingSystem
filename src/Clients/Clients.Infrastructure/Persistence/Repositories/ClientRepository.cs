using Clients.Domain.Entities;
using Clients.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clients.Infrastructure.Persistence.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly ClientsDbContext _context;

    public ClientRepository(ClientsDbContext context)
        => _context = context;

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
        => await _context.Clients.AddAsync(client, cancellationToken);

    public Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Update(client);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
