using System.Text.Json;
using Jotanunes.Application.Services.Analyzers;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class VisionPromptTests
{
    [Fact]
    public void Should_Build_Strict_Schema_With_Nullable_Fields_And_Document_Check()
    {
        using var schema = JsonDocument.Parse(VisionPrompt.Schema(("cpf", "string", "CPF"), ("netPay", "number", "líquido")));
        var root = schema.RootElement;

        Assert.False(root.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(
            ["isExpectedDocument", "detectedDocument", "cpf", "netPay"],
            root.GetProperty("required").EnumerateArray().Select(r => r.GetString()!).ToArray());
        Assert.Equal("boolean", root.GetProperty("properties").GetProperty("isExpectedDocument").GetProperty("type").GetString());
        Assert.Equal(
            ["number", "null"],
            root.GetProperty("properties").GetProperty("netPay").GetProperty("type").EnumerateArray().Select(t => t.GetString()!).ToArray());
    }

    [Fact]
    public void Should_Tell_The_Model_Not_To_Guess()
    {
        var instructions = VisionPrompt.Instructions("CRF do FGTS", "cnpj: o CNPJ");

        Assert.Contains("O documento esperado é: CRF do FGTS.", instructions);
        Assert.Contains("Não deduza", instructions);
        Assert.Contains("Use null", instructions);
        Assert.Contains("- cnpj: o CNPJ", instructions);
    }

    [Fact]
    public void Should_Treat_Missing_Document_Check_As_Not_Expected()
    {
        using var result = JsonDocument.Parse("{}");

        Assert.False(VisionPrompt.IsExpected(result.RootElement));
    }
}
