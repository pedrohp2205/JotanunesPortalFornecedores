using Jotanunes.AppHost.Extensions;
using Jotanunes.AppHost.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddOptions<AppSettings>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var appSettings = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AppSettings>>().Value;


var sqlPassword = builder.AddParameter("sql-password", secret: true);

var sql = builder.AddSqlServer("jotanunes-sql", password: sqlPassword, port: appSettings.SqlServerPort)
    .WithEndpointProxySupport(false)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var jotanunesDb = sql.AddDatabase("jotanunes-dev-db", "jotanunes_portal")
    .WithAutoApplyEfMigrations()
    .WithCommandApplyEfMigrations();

sql.AddDatabase("jotanunes-test-external-db", "jotanunes_portal_test_external")
    .WithAutoApplyEfMigrations()
    .WithCommandApplyEfMigrations();

sql.AddDatabase("jotanunes-test-internal-db", "jotanunes_portal_test_internal")
    .WithAutoApplyEfMigrations()
    .WithCommandApplyEfMigrations();



builder.AddProject<Projects.Jotanunes_API_Internal>("jotanunes-api-internal")
    .WithExternalHttpEndpoints()
    .WithReference(jotanunesDb).WaitFor(jotanunesDb)
    .WithEnvironment("ConnectionStrings__ConnectionString", jotanunesDb)
    .WithHttpHealthCheck("/api/Health");



builder.AddProject<Projects.Jotanunes_API_External>("jotanunes-api-external")
    .WithExternalHttpEndpoints()
    .WithReference(jotanunesDb).WaitFor(jotanunesDb)
    .WithEnvironment("ConnectionStrings__ConnectionString", jotanunesDb)
    .WithHttpHealthCheck("/api/Health");


await builder.Build().RunAsync();
