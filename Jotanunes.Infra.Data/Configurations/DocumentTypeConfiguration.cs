using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.ToTable("document_types");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(d => d.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.AppliesTo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.Subject)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.RequiresExpirationDate)
            .IsRequired();

        builder.Property(d => d.IsConditional)
            .IsRequired();

        builder.Property(d => d.ConditionDescription)
            .HasMaxLength(300);

        builder.Property(d => d.Active)
            .IsRequired();

        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");
    }
}
