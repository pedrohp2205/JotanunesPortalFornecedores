using System.Linq.Expressions;
using Jotanunes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Infra.Data.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<SupplierUser> SupplierUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyGlobalFilter<BaseEntity>(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        var currentTime = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = currentTime;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = currentTime;
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAt = currentTime;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    // Exclusão lógica: toda entidade que herda de BaseEntity passa a ser
    // consultada apenas quando DeletedAt está nulo.
    private static void ApplyGlobalFilter<TBaseEntity>(ModelBuilder modelBuilder) where TBaseEntity : class
    {
        var baseEntityType = typeof(TBaseEntity);
        var derivedEntityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.ClrType != baseEntityType && baseEntityType.IsAssignableFrom(t.ClrType));

        foreach (var entityType in derivedEntityTypes)
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.PropertyOrField(parameter, "DeletedAt");
            var nullValue = Expression.Constant(null);
            var body = Expression.Equal(property, nullValue);
            var lambda = Expression.Lambda(body, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
