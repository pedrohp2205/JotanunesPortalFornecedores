using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class SupplierUserConfiguration : IEntityTypeConfiguration<SupplierUser>
{
    public void Configure(EntityTypeBuilder<SupplierUser> builder)
    {
        builder.ToTable("supplier_users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Active)
            .IsRequired();

        builder.Property(u => u.MustChangePassword)
            .IsRequired();

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(255);

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasIndex(u => u.RefreshToken);
    }
}
