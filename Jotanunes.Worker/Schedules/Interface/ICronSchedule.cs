namespace Jotanunes.Worker.Schedules.Interface;

public interface ICronSchedule
{
    bool IsEnabled { get; }

    DateTimeOffset GetNextOccurrence(DateTimeOffset now);
}
