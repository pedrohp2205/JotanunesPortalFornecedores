using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.RejectionReason)
            .HasMaxLength(500);

        builder.HasIndex(d => new { d.CompanyId, d.DocumentTypeId, d.WorkerId, d.ReferencePeriodStart });

        builder.HasIndex(d => d.WorkerId);

        builder.HasOne(d => d.Company)
            .WithMany()
            .HasForeignKey(d => d.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DocumentType)
            .WithMany()
            .HasForeignKey(d => d.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.UploadedBySupplierUser)
            .WithMany()
            .HasForeignKey(d => d.UploadedBySupplierUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.SupplyRequest)
            .WithMany()
            .HasForeignKey(d => d.SupplyRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Worker)
            .WithMany()
            .HasForeignKey(d => d.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
