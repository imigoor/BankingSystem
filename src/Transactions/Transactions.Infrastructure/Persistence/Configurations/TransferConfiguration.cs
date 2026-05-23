using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transactions.Domain.Entities;

namespace Transactions.Infrastructure.Persistence.Configurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.SenderUserId)
            .IsRequired();

        builder.Property(t => t.ReceiverUserId)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt);

        builder.HasIndex(t => t.SenderUserId);
        builder.HasIndex(t => t.ReceiverUserId);
    }
}
