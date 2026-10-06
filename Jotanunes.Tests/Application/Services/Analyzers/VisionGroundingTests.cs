using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Services.Analyzers;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Tests.Application.Services.Analyzers;

public class VisionGroundingTests
{
    private const string OcrText = """
        Recibo de Pagamento Empregador CONSTRUTORA EXEMPLO LTDA 11.222.333/0001-81 Competência Julho de 2026
        Empregado MARIA APARECIDA DOS SANTOS CPF: 529.982.247-25 Líquido a Receber 1.900,75
        """;

    private static FieldExtraction Fields(params (string Name, string Value)[] fields) =>
        FieldExtraction.FromFields(fields.Select(f => new ExtractedField(f.Name, f.Value)));

    [Fact]
    public void Should_Confirm_Values_Present_In_The_Ocr_Text_Regardless_Of_Formatting()
    {
        var extraction = Fields(("employeeCpf", "52998224725"), ("employerCnpj", "11222333000181"), ("netPay", "1900.75"), ("competence", "07/2026"));

        Assert.Null(VisionGrounding.Check(extraction, new DocumentText([OcrText])));
    }

    [Fact]
    public void Should_Warn_About_Values_Missing_From_The_Ocr_Text()
    {
        var extraction = Fields(("employeeCpf", "11144477735"), ("netPay", "1900.75"));

        var finding = VisionGrounding.Check(extraction, new DocumentText([OcrText]));

        Assert.NotNull(finding);
        Assert.Equal(VisionGrounding.NotConfirmedCode, finding.Code);
        Assert.Equal(FindingSeverity.Warning, finding.Severity);
        Assert.Contains("11144477735", finding.Message);
        Assert.DoesNotContain("1900.75", finding.Message);
    }

    [Fact]
    public void Should_Skip_Short_Hidden_And_List_Values()
    {
        var extraction = Fields(("declaredWorkers", "14"), ("payeeCpf", "***111222**"), ("workers", """[{"cpf":"11144477735"}]"""), ("documentKind", "PAYMENT_RECEIPT"));

        Assert.Null(VisionGrounding.Check(extraction, new DocumentText([OcrText])));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("página sem números legíveis 123")]
    public void Should_Not_Judge_Without_Enough_Ocr_Text(string? ocr)
    {
        var text = ocr is null ? null : new DocumentText([ocr]);

        Assert.Null(VisionGrounding.Check(Fields(("employeeCpf", "11144477735")), text));
    }
}
