using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jotanunes.Application.Services.Analyzers;

public static class VisionPrompt
{
    public const string IsExpectedDocument = "isExpectedDocument";
    public const string DetectedDocument = "detectedDocument";

    public static string Instructions(string expectedDocument, params string[] fieldGuidance)
    {
        var guidance = string.Join(Environment.NewLine, fieldGuidance.Select(g => $"- {g}"));

        return $"""
            Você transcreve dados de documentos brasileiros enviados por fornecedores de uma construtora.
            O documento esperado é: {expectedDocument}.

            Regras:
            - Transcreva exatamente o que está escrito. Não deduza, não complete e não corrija nenhum valor.
            - Use null em qualquer campo ausente, ilegível ou sobre o qual você não tenha certeza.
            - Datas no formato dd/MM/yyyy. Competência no formato MM/yyyy.
            - CPF e CNPJ como aparecem no documento.
            - Valores em reais como número, com ponto como separador decimal (ex.: 1900.75).
            - Em {IsExpectedDocument}, responda false se as imagens não forem o documento esperado, e descreva em {DetectedDocument} o que elas são (ex.: "CNH", "comprovante de endereço").

            Campos:
            {guidance}
            """;
    }

    public static (string Name, JsonNode Schema) List(string name, string description, params (string Name, string Type, string Description)[] itemFields)
    {
        var properties = new JsonObject();
        foreach (var (itemName, type, itemDescription) in itemFields)
        {
            properties[itemName] = Nullable(type, itemDescription);
        }

        return (name, new JsonObject
        {
            ["type"] = new JsonArray("array", "null"),
            ["description"] = description,
            ["items"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new JsonArray(properties.Select(p => (JsonNode)p.Key).ToArray()),
                ["additionalProperties"] = false
            }
        });
    }

    public static string Schema(params (string Name, string Type, string Description)[] fields)
    {
        return Schema(fields.Select(f => (f.Name, (JsonNode)Nullable(f.Type, f.Description))).ToArray());
    }

    public static string Schema(params (string Name, JsonNode Schema)[] fields)
    {
        var properties = new JsonObject
        {
            [IsExpectedDocument] = new JsonObject
            {
                ["type"] = "boolean",
                ["description"] = "true se as imagens são o documento esperado"
            },
            [DetectedDocument] = Nullable("string", "o que o documento é, em poucas palavras")
        };

        foreach (var (name, fieldSchema) in fields)
        {
            properties[name] = fieldSchema;
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = new JsonArray(properties.Select(p => (JsonNode)p.Key).ToArray()),
            ["additionalProperties"] = false
        };

        return schema.ToJsonString();
    }

    public static bool IsExpected(JsonElement result)
    {
        return result.TryGetProperty(IsExpectedDocument, out var value) && value.ValueKind == JsonValueKind.True;
    }

    public static string? Detected(JsonElement result)
    {
        return GetString(result, DetectedDocument);
    }

    public static string? GetString(JsonElement result, string name)
    {
        if (!result.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString()!.Trim(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    public static decimal? GetDecimal(JsonElement result, string name)
    {
        if (!result.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
    }

    public static IEnumerable<JsonElement> GetList(JsonElement result, string name)
    {
        return result.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object).ToList()
            : [];
    }

    public static bool? GetBool(JsonElement result, string name)
    {
        if (!result.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    public static JsonObject Nullable(string type, string description)
    {
        return new JsonObject
        {
            ["type"] = new JsonArray(type, "null"),
            ["description"] = description
        };
    }
}
