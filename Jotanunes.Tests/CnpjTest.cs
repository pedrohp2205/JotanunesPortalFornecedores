using Jotanunes.Domain.Validation;

namespace Jotanunes.Tests;

public class CnpjTest
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Should_Accept_Valid_Cnpj(string cnpj)
    {
        Assert.True(Cnpj.IsValid(cnpj));
    }

    [Theory]
    [InlineData("11222333000182")]
    [InlineData("11111111111111")]
    [InlineData("1122233300018")]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Reject_Invalid_Cnpj(string? cnpj)
    {
        Assert.False(Cnpj.IsValid(cnpj));
    }

    [Fact]
    public void Should_Normalize_Removing_Mask()
    {
        Assert.Equal("11222333000181", Cnpj.Normalize("11.222.333/0001-81"));
    }

    [Fact]
    public void Should_Format_With_Mask()
    {
        Assert.Equal("11.222.333/0001-81", Cnpj.Format("11222333000181"));
    }
}
