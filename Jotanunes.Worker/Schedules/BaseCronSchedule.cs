using Cronos;
using Jotanunes.Worker.Schedules.Interface;

namespace Jotanunes.Worker.Schedules;

public abstract class BaseCronSchedule : ICronSchedule
{
    private readonly CronExpression? _expression;
    private readonly TimeZoneInfo _timeZone;
    private readonly bool _isEnabled;

    private static readonly string[] CronDisabled = ["-", "never"];

    protected BaseCronSchedule(string cron)
    {
        if (string.IsNullOrWhiteSpace(cron))
        {
            throw new InvalidOperationException("Cron não configurado.");
        }

        _isEnabled = !CronDisabled.Any(s => cron.Equals(s, StringComparison.OrdinalIgnoreCase));
        _expression = _isEnabled ? Parse(cron) : null;
        _timeZone = TimeZoneInfo.Local;
    }

    public bool IsEnabled => _isEnabled;

    public DateTimeOffset GetNextOccurrence(DateTimeOffset now)
    {
        var next = _expression?.GetNextOccurrence(now, _timeZone);

        return next ?? throw new InvalidOperationException("Não foi possível determinar a próxima execução da tarefa agendada.");
    }

    private static CronExpression Parse(string cron)
    {
        var fields = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return CronExpression.Parse(cron, fields == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard);
    }
}
