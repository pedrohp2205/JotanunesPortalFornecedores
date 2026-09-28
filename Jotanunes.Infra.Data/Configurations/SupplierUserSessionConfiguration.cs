using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class SupplierUserSessionConfiguration : IEntityTypeConfiguration<SupplierUserSession>
{
    public void Configure(EntityTypeBuilder<SupplierUserSession> builder)
    {
        builder.ToTable("supplier_user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RefreshTokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(s => s.PreviousRefreshTokenHash)
            .HasMaxLength(64);

        builder.Property(s => s.SecurityStamp)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(s => s.RefreshTokenHash)
            .IsUnique();

        builder.HasIndex(s => s.PreviousRefreshTokenHash);

        builder.HasOne(s => s.SupplierUser)
            .WithMany()
            .HasForeignKey(s => s.SupplierUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
