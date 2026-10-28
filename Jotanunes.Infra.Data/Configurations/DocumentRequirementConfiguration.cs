using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class DocumentRequirementConfiguration : IEntityTypeConfiguration<DocumentRequirement>
{
    public void Configure(EntityTypeBuilder<DocumentRequirement> builder)
    {
        builder.ToTable("document_requirements");

        builder.HasKey(r => r.Id);

        // O filtro deixa reexigir um tipo que foi retirado antes: a linha antiga fica com DeletedAt.
        builder.HasIndex(r => new { r.SupplyRequestId, r.DocumentTypeId })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasOne(r => r.SupplyRequest)
            .WithMany(sr => sr.RequiredDocuments)
            .HasForeignKey(r => r.SupplyRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.DocumentType)
            .WithMany()
            .HasForeignKey(r => r.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
