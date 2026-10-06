namespace Jotanunes.Application.Services.Analyzers;

public static class SalaryCalendar
{
    private static readonly (int Month, int Day)[] NationalHolidays =
    [
        (1, 1), (4, 21), (5, 1), (9, 7), (10, 12), (11, 2), (11, 15), (11, 20), (12, 25)
    ];

    public static DateOnly FifthBusinessDayAfter(DateOnly competence)
    {
        var day = new DateOnly(competence.Year, competence.Month, 1).AddMonths(1);
        var businessDays = 0;

        while (true)
        {
            if (IsBusinessDay(day) && ++businessDays == 5)
            {
                return day;
            }

            day = day.AddDays(1);
        }
    }

    private static bool IsBusinessDay(DateOnly day)
    {
        return day.DayOfWeek != DayOfWeek.Sunday && !NationalHolidays.Contains((day.Month, day.Day));
    }
}
