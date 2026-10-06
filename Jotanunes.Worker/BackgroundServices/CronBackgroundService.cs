using Jotanunes.Worker.Schedules.Interface;

namespace Jotanunes.Worker.BackgroundServices;

public abstract class CronBackgroundService(
    ICronSchedule cronSchedule,
    ILogger logger) : BackgroundService
{
    protected abstract string JobName { get; }

    protected abstract Task ExecuteJobAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!cronSchedule.IsEnabled)
        {
            logger.LogWarning("Worker - {JobName} desabilitado.", JobName);
            return;
        }

        logger.LogWarning("Worker - {JobName} iniciado.", JobName);

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset nextRun;

            try
            {
                nextRun = cronSchedule.GetNextOccurrence(DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Falha ao calcular a próxima execução de {JobName}.", JobName);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                continue;
            }

            var delay = nextRun - DateTimeOffset.Now;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ExecuteJobAsync(stoppingToken);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Execução de {JobName} cancelada.", JobName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro durante execução de {JobName}.", JobName);
            }
        }
    }
}
