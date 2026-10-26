using System.Reflection;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Infra.IoC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jotanunes.Worker.Tests.Infrastructure;

public class DocumentAnalysisRegistrationTests
{
    private static ServiceProvider Build(params (string Key, string Value)[] settings)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:ConnectionString"] = "Server=localhost;Database=registration_test;TrustServerCertificate=True",
            ["S3:BucketName"] = "bucket",
            ["S3:Region"] = "auto"
        };
        foreach (var (key, value) in settings)
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static object? VisionClientOf(IDocumentAnalysisService service)
    {
        return typeof(DocumentAnalysisService)
            .GetField("_visionClient", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(service);
    }

    [Fact]
    public void Should_Resolve_Analysis_Without_Vision_When_OpenRouter_Is_Disabled()
    {
        using var provider = Build();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IDocumentAnalysisService>();

        Assert.Null(VisionClientOf(service));
        Assert.Null(scope.ServiceProvider.GetService<IVisionClient>());
    }

    [Fact]
    public void Should_Inject_Vision_Client_When_OpenRouter_Is_Enabled()
    {
        using var provider = Build(("OpenRouter:Enabled", "true"), ("OpenRouter:ApiKey", "chave-de-teste"));
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IDocumentAnalysisService>();

        Assert.NotNull(VisionClientOf(service));
    }

    [Fact]
    public void Should_Refuse_To_Start_With_OpenRouter_Enabled_And_No_Key()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(("OpenRouter:Enabled", "true")));

        Assert.Contains("OpenRouter:ApiKey", ex.Message);
    }
}
