using System.Diagnostics;
using Jotanunes.Infra.IoC;
using Jotanunes.Worker.Schedules.Interface;
using OpenTelemetry;

namespace Jotanunes.Worker.BackgroundServices;

public abstract class QueueCronBackgroundService(
    ICronSchedule cronSchedule,
    IServiceScopeFactory scopeFactory,
    ILogger logger) : CronBackgroundService(cronSchedule, logger)
{
    private static readonly ActivitySource ActivitySource = new(DependencyInjectionTelemetry.WorkerActivitySourceName);

    protected abstract int BatchSize { get; }

    protected abstract Task<List<long>> GetPendingIds(IServiceProvider services, int take);

    protected abstract Task Process(IServiceProvider services, long id, CancellationToken stoppingToken);

    protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            List<long> pendingIds;
            using (SuppressInstrumentationScope.Begin())
            using (var scope = scopeFactory.CreateScope())
            {
                pendingIds = await GetPendingIds(scope.ServiceProvider, BatchSize);
            }

            if (pendingIds.Count == 0)
            {
                return;
            }

            var processed = 0;
            foreach (var id in pendingIds)
            {
                using var activity = ActivitySource.StartActivity($"{JobName} process");
                activity?.SetTag("jotanunes.job", JobName);
                activity?.SetTag("jotanunes.item_id", id);

                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await Process(scope.ServiceProvider, id, stoppingToken);
                    processed++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity?.AddException(ex);
                    logger.LogError(ex, "Erro ao processar o item {Id} de {JobName}.", id, JobName);
                }
            }

            if (processed < pendingIds.Count)
            {
                return;
            }
        }
    }
}
