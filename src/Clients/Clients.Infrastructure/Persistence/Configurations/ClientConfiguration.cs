using Clients.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clients.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(c => c.Email).IsUnique();

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.ProfilePictureUrl)
            .HasMaxLength(2048);

        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt);

        builder.OwnsOne(c => c.BankingDetails, bd =>
        {
            bd.Property(b => b.Agency)
                .HasColumnName("Agency")
                .HasMaxLength(10)
                .IsRequired();

            bd.Property(b => b.AccountNumber)
                .HasColumnName("AccountNumber")
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}
