using Jotanunes.Internal.BddTests.Support.Fixtures;
using Jotanunes.API.Internal.Controllers;
using Jotanunes.Application.Interfaces;
using Jotanunes.Infra.Data.Context;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

using AssemblyFixtureAttribute = Reqnroll.xUnit3.ReqnrollPlugin.AssemblyFixtureAttribute;

[assembly: AssemblyFixture(typeof(ApiWebAppFactoryFixture))]

namespace Jotanunes.Internal.BddTests.Support.Fixtures;

internal class ApiWebAppFactoryFixture(
    DatabaseFixture databaseFixture,
    TestSettingsFixture testSettingsFixture)
    : WebApplicationFactory<CompanyController>
{
    public Mock<IDocumentStorageService> MockDocumentStorage { get; init; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:ConnectionString", testSettingsFixture.TestSettings.DataBase.ConnectionString);
        builder.UseSetting("Email:Enabled", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddScoped(_ => databaseFixture.CreateDbContext());

            services.RemoveAll<IDocumentStorageService>();
            services.AddSingleton(_ => MockDocumentStorage.Object);
        });
    }
}
