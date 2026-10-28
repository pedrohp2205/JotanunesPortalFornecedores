namespace Jotanunes.Domain.Time;

// A Jotanunes opera em horário de Brasília, mas tudo é persistido em UTC.
// A conversão acontece em dois pontos: ao expor a data na API e ao decidir
// que dia é "hoje" nas regras de negócio — sem isso, das 21h à meia-noite
// o UTC já está no dia seguinte e vencimentos e períodos pulam um dia.
public static class BrazilTime
{
    private static readonly TimeZoneInfo TimeZone = Resolve();

    public static DateTime FromUtc(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone);
    }

    public static DateTime ToUtc(DateTime local)
    {
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeZone);
    }

    public static DateOnly ToDateOnly(DateTime utc)
    {
        return DateOnly.FromDateTime(FromUtc(utc));
    }

    public static DateOnly Today()
    {
        return ToDateOnly(DateTime.UtcNow);
    }

    public static DateOnly Today(TimeProvider timeProvider)
    {
        return ToDateOnly(timeProvider.GetUtcNow().UtcDateTime);
    }

    // O id IANA funciona no Linux e, desde o .NET 6, também no Windows.
    // O fallback cobre uma imagem sem a base de fusos instalada.
    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "America/Fortaleza", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "America/Fortaleza",
            TimeSpan.FromHours(-3),
            "Horário de Brasília",
            "Horário de Brasília");
    }
}
