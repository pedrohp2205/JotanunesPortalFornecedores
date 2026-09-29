using AwesomeAssertions;

using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.WorkSiteTests;

[Binding]
internal class LeituraWorkSiteStepDefinitions(
    WorkSiteApiClientFixture workSiteFix,
    WorkSiteGivenContext workSiteCtx,
    WorkSiteResultContext workSiteResultCtx)
{
    private List<WorkSiteDto>? _obras;

    [When(@"eu listar as obras")]
    public async Task QuandoEuListarAsObras()
    {
        _obras = await workSiteFix.GetAsync();
    }

    [When(@"eu consultar a obra cadastrada")]
    public async Task QuandoEuConsultarAObraCadastrada()
    {
        workSiteResultCtx.Obra = await workSiteFix.GetByIdAsync(workSiteCtx.IdObraCadastrada!.Value);
    }

    [When(@"eu consultar uma obra inexistente")]
    public async Task QuandoEuConsultarUmaObraInexistente()
    {
        await workSiteFix.GetByIdAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"a listagem de obras contém a obra cadastrada")]
    public void EntaoAListagemDeObrasContemAObraCadastrada()
    {
        _obras.Should().NotBeNull();
        _obras!.Select(o => o.Id).Should().Contain(workSiteCtx.IdObraCadastrada!.Value);
    }

    [Then(@"a obra retornada deve ser a obra cadastrada")]
    public void EntaoAObraRetornadaDeveSerAObraCadastrada()
    {
        workSiteResultCtx.Obra.Should().NotBeNull();
        workSiteResultCtx.Obra!.Id.Should().Be(workSiteCtx.IdObraCadastrada);
    }

    [Then(@"^a obra retornada deve ter o nome ""(.*)"" e renovação a cada (\d+) dias$")]
    public void EntaoAObraRetornadaDeveTerONomeERenovacao(string nome, int periodoRenovacaoDias)
    {
        workSiteResultCtx.Obra.Should().NotBeNull();
        workSiteResultCtx.Obra!.Name.Should().Be(nome);
        workSiteResultCtx.Obra.RenewalPeriodDays.Should().Be(periodoRenovacaoDias);
    }
}
