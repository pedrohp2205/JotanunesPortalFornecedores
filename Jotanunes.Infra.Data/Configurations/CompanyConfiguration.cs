using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jotanunes.Infra.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Cnpj)
            .HasMaxLength(14)
            .IsRequired();

        builder.Property(c => c.CorporateName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.TradeName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.StateRegistration)
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Phone)
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(c => c.ResponsibleName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<int>()
            .IsRequired();

        // Índice único parcial: um CNPJ pode ser reaproveitado depois que o
        // registro anterior for excluído logicamente.
        builder.HasIndex(c => c.Cnpj)
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("AddressStreet").HasMaxLength(200).IsRequired();
            address.Property(a => a.Number).HasColumnName("AddressNumber").HasMaxLength(20).IsRequired();
            address.Property(a => a.Complement).HasColumnName("AddressComplement").HasMaxLength(100);
            address.Property(a => a.Neighborhood).HasColumnName("AddressNeighborhood").HasMaxLength(100).IsRequired();
            address.Property(a => a.City).HasColumnName("AddressCity").HasMaxLength(100).IsRequired();
            address.Property(a => a.State).HasColumnName("AddressState").HasMaxLength(2).IsRequired();
            address.Property(a => a.ZipCode).HasColumnName("AddressZipCode").HasMaxLength(8).IsRequired();
        });

        builder.Navigation(c => c.Address).IsRequired();
    }
}
