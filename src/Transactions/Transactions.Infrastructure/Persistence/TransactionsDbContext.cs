using Microsoft.EntityFrameworkCore;
using Transactions.Domain.Entities;

namespace Transactions.Infrastructure.Persistence;

public class TransactionsDbContext : DbContext
{
    public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : base(options) { }

    public DbSet<Transfer> Transfers => Set<Transfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
