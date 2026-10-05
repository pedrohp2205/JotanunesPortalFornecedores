using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class WorkerAllocationConfiguration : IEntityTypeConfiguration<WorkerAllocation>
{
    public void Configure(EntityTypeBuilder<WorkerAllocation> builder)
    {
        builder.ToTable("worker_allocations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AllocatedAt)
            .IsRequired();

        builder.Ignore(a => a.IsActive);

        builder.HasIndex(a => new { a.SupplyRequestId, a.WorkerId })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL AND [ReleasedAt] IS NULL");

        builder.HasIndex(a => a.WorkerId);

        builder.HasOne(a => a.SupplyRequest)
            .WithMany()
            .HasForeignKey(a => a.SupplyRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Worker)
            .WithMany()
            .HasForeignKey(a => a.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
