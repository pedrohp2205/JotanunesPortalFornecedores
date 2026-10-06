using Jotanunes.Application.Interfaces;
using Jotanunes.Worker.Models;
using Jotanunes.Worker.Schedules.Interface;
using Microsoft.Extensions.Options;

namespace Jotanunes.Worker.BackgroundServices;

public class DocumentAnalysisWorker(
    IDocumentAnalysisCronSchedule cronSchedule,
    IServiceScopeFactory scopeFactory,
    IOptions<DocumentAnalysisSettings> settings,
    ILogger<DocumentAnalysisWorker> logger) : CronBackgroundService(cronSchedule, logger)
{
    protected override string JobName => "DocumentAnalysis";

    protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
    {
        var batchSize = settings.Value.BatchSize;

        while (!stoppingToken.IsCancellationRequested)
        {
            List<long> pendingIds;
            using (var scope = scopeFactory.CreateScope())
            {
                pendingIds = await scope.ServiceProvider.GetRequiredService<IDocumentAnalysisService>().GetPendingIds(batchSize);
            }

            if (pendingIds.Count == 0)
            {
                return;
            }

            var analyzed = 0;
            foreach (var id in pendingIds)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IDocumentAnalysisService>().Analyze(id, stoppingToken);
                    analyzed++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Erro ao gravar a análise {AnalysisId}.", id);
                }
            }

            if (analyzed < pendingIds.Count)
            {
                return;
            }
        }
    }
}
