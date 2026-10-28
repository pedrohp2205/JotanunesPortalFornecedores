using System.Text.Json;
using System.Text.Json.Serialization;
using Jotanunes.Domain.Time;

namespace Jotanunes.API.Shared.Json;

// Na borda da API as datas trafegam em horário de Brasília, já convertidas,
// para o front exibir sem tratar fuso. O armazenamento continua em UTC.
public class BrazilDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();

        return value.Kind == DateTimeKind.Unspecified ? BrazilTime.ToUtc(value) : value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(BrazilTime.FromUtc(value));
    }
}
