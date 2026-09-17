using Amazon;
using Amazon.S3;
using Jotanunes.Application.DTOs.Mapping;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Jotanunes.Infra.Data.Repositories;
using Jotanunes.Infra.Security.Services;
using Jotanunes.Infra.Storage.Services;
using Jotanunes.Infra.Storage.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jotanunes.Infra.IoC;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection service, IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE")
            ?? configuration.GetConnectionString("ConnectionString");

        service.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        service.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

        service.AddScoped<IUnitOfWork, UnitOfWork>();

        service.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        service.AddScoped<ITokenService, JwtTokenService>();

        service.AddScoped<ICompanyService, CompanyService>();
        service.AddScoped<ISupplierUserService, SupplierUserService>();
        service.AddScoped<IAuthService, AuthService>();
        service.AddScoped<IWorkSiteService, WorkSiteService>();
        service.AddScoped<IDocumentTypeService, DocumentTypeService>();
        service.AddScoped<IDocumentService, DocumentService>();
        service.AddScoped<IDocumentComplianceService, DocumentComplianceService>();

        service.AddDocumentStorage(configuration);

        return service;
    }

    private static IServiceCollection AddDocumentStorage(this IServiceCollection service, IConfiguration configuration)
    {
        var settings = configuration.GetSection(S3Settings.SectionName).Get<S3Settings>()
            ?? throw new InvalidOperationException("Seção de configuração 'S3' não encontrada.");

        if (string.IsNullOrWhiteSpace(settings.BucketName))
        {
            throw new InvalidOperationException("S3:BucketName é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(settings.Region))
        {
            throw new InvalidOperationException("S3:Region é obrigatório.");
        }

        service.Configure<S3Settings>(configuration.GetSection(S3Settings.SectionName));

        service.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region)
            };

            if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
            {
                config.ServiceURL = settings.ServiceUrl;
                config.ForcePathStyle = settings.ForcePathStyle;
            }

            return !string.IsNullOrWhiteSpace(settings.AccessKey) && !string.IsNullOrWhiteSpace(settings.SecretKey)
                ? new AmazonS3Client(settings.AccessKey, settings.SecretKey, config)
                : new AmazonS3Client(config);
        });

        service.AddScoped<IDocumentStorageService, S3DocumentStorageService>();

        return service;
    }
}
