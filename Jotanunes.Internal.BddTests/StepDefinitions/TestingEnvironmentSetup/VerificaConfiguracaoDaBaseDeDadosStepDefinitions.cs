using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Support.Fixtures;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.Internal.BddTests.StepDefinitions.TestingEnvironmentSetup;

[Binding]
public class VerificaConfiguracaoDaBaseDeDadosStepDefinitions(DatabaseFixture databaseFixture)
{
    private bool _possuiTransacao;
    private Exception? _erroConsulta;

    [When(@"eu solicitar uma transação à base de dados")]
    public async Task QuandoEuSolicitarUmaTransacaoABaseDeDados()
    {
        await using var context = databaseFixture.CreateDbContext();
        _possuiTransacao = context.Database.CurrentTransaction is not null;
        try
        {
            await context.Companies.AnyAsync();
        }
        catch (Exception ex)
        {
            _erroConsulta = ex;
        }
    }

    [Then(@"a transação deve ser obtida com sucesso")]
    public void EntaoATransacaoDeveSerObtidaComSucesso()
    {
        _possuiTransacao.Should().BeTrue();
        _erroConsulta.Should().BeNull();
    }
}
