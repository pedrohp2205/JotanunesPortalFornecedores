using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class CompanyWorkSiteConfiguration : IEntityTypeConfiguration<CompanyWorkSite>
{
    public void Configure(EntityTypeBuilder<CompanyWorkSite> builder)
    {
        builder.ToTable("company_work_sites");

        builder.HasKey(cw => cw.Id);
        
        builder.HasIndex(cw => new { cw.CompanyId, cw.WorkSiteId })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasOne(cw => cw.Company)
            .WithMany()
            .HasForeignKey(cw => cw.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cw => cw.WorkSite)
            .WithMany(w => w.Companies)
            .HasForeignKey(cw => cw.WorkSiteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
