using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class DocumentAnalysisConfiguration : IEntityTypeConfiguration<DocumentAnalysis>
{
    public void Configure(EntityTypeBuilder<DocumentAnalysis> builder)
    {
        builder.ToTable("document_analyses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Verdict)
            .HasConversion<int?>();

        builder.Property(a => a.Engine)
            .HasConversion<int?>();

        builder.Property(a => a.FailureReason)
            .HasMaxLength(DocumentAnalysis.MaxFailureReasonLength);

        builder.OwnsMany(a => a.Fields, fields => fields.ToJson());

        builder.OwnsMany(a => a.Findings, findings =>
        {
            findings.ToJson();
            findings.Property(f => f.Severity).HasConversion<int>();
        });

        builder.HasIndex(a => a.DocumentId)
            .IsUnique();

        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Document)
            .WithMany()
            .HasForeignKey(a => a.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
