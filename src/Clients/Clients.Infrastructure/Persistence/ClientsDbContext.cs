using Clients.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clients.Infrastructure.Persistence;

public class ClientsDbContext : DbContext
{
    public ClientsDbContext(DbContextOptions<ClientsDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
