using Jotanunes.Application.Services.Analyzers;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class SalaryCalendarTests
{
    [Theory]
    [InlineData(2026, 7, 2026, 8, 6)]
    [InlineData(2026, 8, 2026, 9, 5)]
    [InlineData(2026, 10, 2026, 11, 7)]
    [InlineData(2026, 12, 2027, 1, 7)]
    public void Should_Count_Saturdays_And_Skip_Sundays_And_National_Holidays(int year, int month, int dueYear, int dueMonth, int dueDay)
    {
        Assert.Equal(new DateOnly(dueYear, dueMonth, dueDay), SalaryCalendar.FifthBusinessDayAfter(new DateOnly(year, month, 1)));
    }
}
