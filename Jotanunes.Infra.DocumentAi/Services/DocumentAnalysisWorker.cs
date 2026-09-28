using Jotanunes.Application.Interfaces;
using Jotanunes.Infra.DocumentAi.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jotanunes.Infra.DocumentAi.Services;

public class DocumentAnalysisWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DocumentAnalysisSettings _settings;
    private readonly ILogger<DocumentAnalysisWorker> _logger;

    public DocumentAnalysisWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<DocumentAnalysisSettings> settings,
        ILogger<DocumentAnalysisWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _settings.PollingIntervalSeconds));
        var batchSize = Math.Max(1, _settings.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;

            try
            {
                processed = await ProcessBatch(batchSize, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erro no ciclo de análise de documentos.");
            }

            if (processed < batchSize)
            {
                await Task.Delay(interval, stoppingToken);
            }
        }
    }

    private async Task<int> ProcessBatch(int batchSize, CancellationToken stoppingToken)
    {
        List<long> pendingIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            pendingIds = await scope.ServiceProvider.GetRequiredService<IDocumentAnalysisService>().GetPendingIds(batchSize);
        }

        var analyzed = 0;

        foreach (var id in pendingIds)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IDocumentAnalysisService>().Analyze(id, stoppingToken);
                analyzed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Erro ao gravar a análise {AnalysisId}.", id);
            }
        }

        return analyzed;
    }
}
