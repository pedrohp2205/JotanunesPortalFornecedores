using Jotanunes.Internal.BddTests.Support.Models;
using Jotanunes.Infra.Data.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jotanunes.Internal.BddTests.Support;

public static class TestConfigurationHelper
{
    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<TestSettings>()
            .AddEnvironmentVariables()
            .Build();
    }

    public static TestSettings GetTestSettings(IConfiguration configuration)
    {
        var testSettings = new TestSettings();
        configuration.Bind(testSettings);
        return testSettings;
    }

    public static void ApplyMigrations(DataBaseSettings dataBaseSettings)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(dataBaseSettings.ConnectionString)
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Database.Migrate();
    }
}
