using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class SupplyRequestConfiguration : IEntityTypeConfiguration<SupplyRequest>
{
    public void Configure(EntityTypeBuilder<SupplyRequest> builder)
    {
        builder.ToTable("supply_requests");

        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.SupplierType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(sr => sr.Status)
            .HasConversion<int>()
            .IsRequired();

        // Uma solicitação ativa (Open/InProgress) por empresa, obra e tipo de fornecimento.
        // Solicitações encerradas não bloqueiam a abertura de uma nova.
        builder.HasIndex(sr => new { sr.CompanyId, sr.WorkSiteId, sr.SupplierType })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL AND [Status] IN (1, 2)");

        builder.HasIndex(sr => sr.Status);

        builder.HasOne(sr => sr.Company)
            .WithMany()
            .HasForeignKey(sr => sr.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sr => sr.WorkSite)
            .WithMany(w => w.SupplyRequests)
            .HasForeignKey(sr => sr.WorkSiteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
