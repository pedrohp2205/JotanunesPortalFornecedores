using Jotanunes.Application.Interfaces;
using Jotanunes.Worker.Models;
using Jotanunes.Worker.Schedules.Interface;
using Microsoft.Extensions.Options;

namespace Jotanunes.Worker.BackgroundServices;

public class DocumentAnalysisWorker(
    IDocumentAnalysisCronSchedule cronSchedule,
    IServiceScopeFactory scopeFactory,
    IOptions<DocumentAnalysisSettings> settings,
    ILogger<DocumentAnalysisWorker> logger) : QueueCronBackgroundService(cronSchedule, scopeFactory, logger)
{
    protected override string JobName => "DocumentAnalysis";

    protected override int BatchSize => settings.Value.BatchSize;

    protected override Task<List<long>> GetPendingIds(IServiceProvider services, int take)
    {
        return services.GetRequiredService<IDocumentAnalysisService>().GetPendingIds(take);
    }

    protected override Task Process(IServiceProvider services, long id, CancellationToken stoppingToken)
    {
        return services.GetRequiredService<IDocumentAnalysisService>().Analyze(id, stoppingToken);
    }
}
