using Amazon;
using Amazon.S3;
using Jotanunes.Application.DTOs.Mapping;
using Jotanunes.Application.Interfaces;
using Jotanunes.Application.Services;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Application.Services.Compliance;
using Jotanunes.Application.Settings;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Infra.Data.Context;
using Jotanunes.Infra.Data.Repositories;
using Jotanunes.Infra.DocumentAi.Services;
using Jotanunes.Infra.DocumentAi.Settings;
using Jotanunes.Infra.Email.Services;
using Jotanunes.Infra.Email.Settings;
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
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        service.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

        service.AddScoped<IUnitOfWork, UnitOfWork>();

        service.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        service.AddScoped<ITokenService, JwtTokenService>();

        service.AddScoped<ICompanyService, CompanyService>();
        service.AddScoped<ISupplierUserService, SupplierUserService>();
        service.AddScoped<IAuthService, AuthService>();
        service.AddScoped<IWorkSiteService, WorkSiteService>();
        service.AddScoped<ISupplyRequestService, SupplyRequestService>();
        service.AddScoped<IDocumentTypeService, DocumentTypeService>();
        service.AddScoped<IDocumentService, DocumentService>();
        service.AddScoped<IDocumentComplianceService, DocumentComplianceService>();
        service.AddScoped<IWorkerService, WorkerService>();

        service.AddDocumentStorage(configuration);
        service.AddEmail(configuration);
        service.AddDocumentAnalysis(configuration);

        return service;
    }

    private static IServiceCollection AddDocumentAnalysis(this IServiceCollection service, IConfiguration configuration)
    {
        var openRouterSection = configuration.GetSection(OpenRouterSettings.SectionName);
        var openRouter = openRouterSection.Get<OpenRouterSettings>() ?? new OpenRouterSettings();

        service.Configure<OpenRouterSettings>(openRouterSection);
        service.Configure<DocumentImageSettings>(configuration.GetSection(DocumentImageSettings.SectionName));
        var ocrSection = configuration.GetSection(OcrSettings.SectionName);
        service.Configure<OcrSettings>(ocrSection);
        service.AddSingleton(TimeProvider.System);

        service.AddSingleton<IDocumentTextExtractor, PdfNativeTextExtractor>();
        service.AddSingleton<PdfiumDocumentRasterizer>();
        service.AddSingleton<IDocumentRasterizer>(provider => provider.GetRequiredService<PdfiumDocumentRasterizer>());

        if ((ocrSection.Get<OcrSettings>() ?? new OcrSettings()).Enabled)
        {
            service.AddSingleton<ITesseractRunner, TesseractCliRunner>();
            service.AddSingleton<IDocumentTextExtractor, TesseractOcrExtractor>();
        }

        if (openRouter.Enabled)
        {
            if (string.IsNullOrWhiteSpace(openRouter.ApiKey))
            {
                throw new InvalidOperationException("OpenRouter:ApiKey é obrigatório quando OpenRouter:Enabled=true.");
            }

            service.AddHttpClient<IVisionClient, OpenRouterVisionClient>(client =>
            {
                client.BaseAddress = new Uri(openRouter.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(openRouter.TimeoutSeconds);
            });
        }

        service.AddSingleton<IDocumentTypeAnalyzer, CrfAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, PaymentReceiptAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, PaymentProofAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, FgtsPaymentProofAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, FgtsDetailAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, FgtsGuideAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, TimesheetAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, EmployeeListAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, CnpjCardAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, FederalCndAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, SimplesNacionalAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, DctfWebAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, PayrollAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, SocialContractAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, AddressProofAnalyzer>();
        service.AddSingleton<IDocumentTypeAnalyzer, PartnerIdAnalyzer>();
        foreach (var analyzer in GenericCertificateAnalyzer.Defaults())
        {
            service.AddSingleton<IDocumentTypeAnalyzer>(analyzer);
        }

        service.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
        service.AddScoped<IPeriodComplianceService, PeriodComplianceService>();

        return service;
    }

    private static IServiceCollection AddEmail(this IServiceCollection service, IConfiguration configuration)
    {
        var section = configuration.GetSection(EmailSettings.SectionName);
        var settings = section.Get<EmailSettings>() ?? new EmailSettings();

        service.Configure<EmailSettings>(section);
        service.Configure<NotificationSettings>(section);

        if (settings.Enabled)
        {
            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new InvalidOperationException("Email:ApiKey é obrigatório quando Email:Enabled=true.");
            }

            if (string.IsNullOrWhiteSpace(settings.FromAddress))
            {
                throw new InvalidOperationException("Email:FromAddress é obrigatório quando Email:Enabled=true.");
            }

            // O envio é síncrono na request; timeout curto para não travar a API se o provedor estiver lento.
            service.AddHttpClient<IEmailSender, ResendEmailSender>(client => client.Timeout = TimeSpan.FromSeconds(15));
        }
        else
        {
            service.AddScoped<IEmailSender, LogEmailSender>();
        }

        service.AddScoped<ISupplierNotificationService, SupplierNotificationService>();

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
