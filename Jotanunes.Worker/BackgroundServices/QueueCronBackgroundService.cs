using Jotanunes.Worker.Schedules.Interface;

namespace Jotanunes.Worker.BackgroundServices;

public abstract class QueueCronBackgroundService(
    ICronSchedule cronSchedule,
    IServiceScopeFactory scopeFactory,
    ILogger logger) : CronBackgroundService(cronSchedule, logger)
{
    protected abstract int BatchSize { get; }

    protected abstract Task<List<long>> GetPendingIds(IServiceProvider services, int take);

    protected abstract Task Process(IServiceProvider services, long id, CancellationToken stoppingToken);

    protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            List<long> pendingIds;
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
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await Process(scope.ServiceProvider, id, stoppingToken);
                    processed++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
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
