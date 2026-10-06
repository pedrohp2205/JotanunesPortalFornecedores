using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class PeriodComplianceReportConfiguration : IEntityTypeConfiguration<PeriodComplianceReport>
{
    public void Configure(EntityTypeBuilder<PeriodComplianceReport> builder)
    {
        builder.ToTable("period_compliance_reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.Verdict)
            .HasConversion<int?>();

        builder.OwnsMany(r => r.Findings, findings =>
        {
            findings.ToJson();
            findings.Property(f => f.Severity).HasConversion<int>();
        });

        builder.HasIndex(r => new { r.SupplyRequestId, r.PeriodStart, r.PeriodEnd })
            .IsUnique();

        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.SupplyRequest)
            .WithMany()
            .HasForeignKey(r => r.SupplyRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
