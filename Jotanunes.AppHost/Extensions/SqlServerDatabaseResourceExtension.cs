using Jotanunes.Infra.Data.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jotanunes.AppHost.Extensions;

public static class SqlServerDatabaseResourceExtension
{
    public static IResourceBuilder<SqlServerDatabaseResource> WithAutoApplyEfMigrations(
        this IResourceBuilder<SqlServerDatabaseResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.OnResourceReady(async (db, @event, ct) =>
        {
            var logger = @event.Services.GetRequiredService<ILogger<Program>>();
            var connectionString = await GetConnectionStringAsync(db, ct);
            await ApplyEfMigrationsAsync(db, connectionString, logger, ct);
        });
    }


    public static IResourceBuilder<SqlServerDatabaseResource> WithCommandApplyEfMigrations(
        this IResourceBuilder<SqlServerDatabaseResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithCommand(
            name: "apply-ef-migrations",
            displayName: "Apply EF Migrations",
            executeCommand: async context =>
            {
                var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
                try
                {
                    var db = builder.Resource;
                    var connectionString = await GetConnectionStringAsync(db, context.CancellationToken);
                    await ApplyEfMigrationsAsync(db, connectionString, logger, context.CancellationToken);
                }
                catch (Exception ex)
                {
                    return CommandResults.Failure(ex.Message);
                }
                return CommandResults.Success();
            },
            commandOptions: new CommandOptions
            {
                IconName = "DatabaseArrowUp",
                IconVariant = IconVariant.Filled,
                Description = "Apply EF Migrations"
            }
        );
    }


    private static async Task ApplyEfMigrationsAsync(
        SqlServerDatabaseResource db,
        string connectionString,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        try
        {
            await using var context = new ApplicationDbContext(options);
            var pending = (await context.Database.GetPendingMigrationsAsync(ct)).ToList();
            await context.Database.MigrateAsync(ct);

            logger.LogInformation("Successfully applied {Count} database migrations to '{Database}'. {Migrations}",
                pending.Count, db.DatabaseName, string.Join(", ", pending));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations to '{Database}'.", db.DatabaseName);
            throw new DistributedApplicationException($"Failed to apply database migrations to '{db.DatabaseName}'. See logs for details.", ex);
        }
    }

    private static async Task<string> GetConnectionStringAsync(SqlServerDatabaseResource db, CancellationToken ct)
    {
        var connectionString = await db.ConnectionStringExpression.GetValueAsync(ct);
        if (connectionString == null)
        {
            throw new DistributedApplicationException($"The '{db.DatabaseName}' resource connection string was null.");
        }
        return connectionString;
    }
}
