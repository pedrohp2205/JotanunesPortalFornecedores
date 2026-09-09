using Jotanunes.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jotanunes.Infra.IoC;

public static class DependencyInjectionMigration
{
    public static IServiceCollection ApplyPendingMigrations(this IServiceCollection service)
    {
        using (var serviceProvider = service.BuildServiceProvider())
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (dbContext.Database.GetPendingMigrations().Any())
            {
                dbContext.Database.Migrate();
            }
        }
        return service;
    }
}
