using Jotanunes.Infra.IoC;
using Jotanunes.Worker.BackgroundServices;
using Jotanunes.Worker.Models;
using Jotanunes.Worker.Schedules;
using Jotanunes.Worker.Schedules.Interface;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Services.AddOptions<WorkerSettings>()
    .BindConfiguration(WorkerSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DocumentAnalysisSettings>()
    .BindConfiguration(DocumentAnalysisSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<PeriodComplianceSettings>()
    .BindConfiguration(PeriodComplianceSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSingleton<IDocumentAnalysisCronSchedule, DocumentAnalysisCronSchedule>();
builder.Services.AddSingleton<IPeriodComplianceCronSchedule, PeriodComplianceCronSchedule>();

builder.Services.AddHostedService<DocumentAnalysisWorker>();
builder.Services.AddHostedService<PeriodComplianceWorker>();

var host = builder.Build();

await host.RunAsync();
