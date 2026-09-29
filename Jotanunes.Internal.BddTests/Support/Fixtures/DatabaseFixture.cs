using System.Data.Common;

using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Fixtures;
using Jotanunes.Infra.Data.Context;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using AssemblyFixtureAttribute = Reqnroll.xUnit3.ReqnrollPlugin.AssemblyFixtureAttribute;

[assembly: AssemblyFixture(typeof(DatabaseFixture))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Jotanunes.Internal.BddTests.Support.Fixtures;

[Binding]
public class DatabaseFixture(TestSettingsFixture testSettingsFixture) : IDisposable
{
    private SqlConnection? _connection;
    private DbTransaction? _scenarioTransaction;

    public DbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                var dataBaseSettings = testSettingsFixture.TestSettings.DataBase;
                TestConfigurationHelper.ApplyMigrations(dataBaseSettings);

                _connection = new SqlConnection(dataBaseSettings.ConnectionString);
                _connection.Open();
            }
            return _connection;
        }
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(Connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.UseTransaction(_scenarioTransaction);
        return context;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scenarioTransaction?.Dispose();
            _scenarioTransaction = null;

            _connection?.Dispose();
            _connection = null;
        }
    }

    [BeforeScenario]
    public void SetupTest()
    {
        _scenarioTransaction = Connection.BeginTransaction();
    }

    [AfterScenario]
    public void TeardownTest()
    {
        _scenarioTransaction?.Rollback();
        _scenarioTransaction?.Dispose();
        _scenarioTransaction = null;
    }
}
