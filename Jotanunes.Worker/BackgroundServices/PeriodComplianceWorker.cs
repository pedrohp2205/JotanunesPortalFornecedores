using Jotanunes.Application.Interfaces;
using Jotanunes.Worker.Models;
using Jotanunes.Worker.Schedules.Interface;
using Microsoft.Extensions.Options;

namespace Jotanunes.Worker.BackgroundServices;

public class PeriodComplianceWorker(
    IPeriodComplianceCronSchedule cronSchedule,
    IServiceScopeFactory scopeFactory,
    IOptions<PeriodComplianceSettings> settings,
    ILogger<PeriodComplianceWorker> logger) : QueueCronBackgroundService(cronSchedule, scopeFactory, logger)
{
    protected override string JobName => "PeriodCompliance";

    protected override int BatchSize => settings.Value.BatchSize;

    protected override Task<List<long>> GetPendingIds(IServiceProvider services, int take)
    {
        return services.GetRequiredService<IPeriodComplianceService>().GetPendingIds(take);
    }

    protected override Task Process(IServiceProvider services, long id, CancellationToken stoppingToken)
    {
        return services.GetRequiredService<IPeriodComplianceService>().Recalculate(id, stoppingToken);
    }
}
