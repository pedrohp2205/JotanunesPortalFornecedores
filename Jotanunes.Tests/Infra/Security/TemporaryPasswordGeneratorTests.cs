using Jotanunes.Infra.Security.Services;

namespace Jotanunes.Tests.Infra.Security;

public class TemporaryPasswordGeneratorTests
{
    private readonly TemporaryPasswordGenerator _generator = new();

    [Fact]
    public void Should_Generate_Password_Accepted_By_The_Create_And_Reset_Endpoints()
    {
        var password = _generator.Generate();

        Assert.True(password.Length >= 8);
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsDigit);
        Assert.Contains(password, c => "!@#$%&*".Contains(c));
    }

    [Fact]
    public void Should_Not_Use_Ambiguous_Characters()
    {
        for (var i = 0; i < 200; i++)
        {
            Assert.DoesNotContain(_generator.Generate(), c => "O0lI1".Contains(c));
        }
    }

    [Fact]
    public void Should_Generate_A_Different_Password_Every_Time()
    {
        var passwords = Enumerable.Range(0, 200).Select(_ => _generator.Generate()).ToList();

        Assert.Equal(passwords.Count, passwords.Distinct().Count());
    }
}
