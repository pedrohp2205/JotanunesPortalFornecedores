using Jotanunes.Domain.Validation;

namespace Jotanunes.Tests;

public class CpfTest
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void Should_Accept_Valid_Cpf(string cpf)
    {
        Assert.True(Cpf.IsValid(cpf));
    }

    [Theory]
    [InlineData("529.982.247-26")]
    [InlineData("11111111111")]
    [InlineData("5299822472")]
    [InlineData("")]
    [InlineData(null)]
    public void Should_Reject_Invalid_Cpf(string? cpf)
    {
        Assert.False(Cpf.IsValid(cpf));
    }

    [Fact]
    public void Should_Normalize_Removing_Mask()
    {
        Assert.Equal("52998224725", Cpf.Normalize("529.982.247-25"));
    }

    [Fact]
    public void Should_Format_With_Mask()
    {
        Assert.Equal("529.982.247-25", Cpf.Format("52998224725"));
    }
}
