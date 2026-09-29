using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.CompanyTests;

[Binding]
internal class AtualizacaoCompanyStepDefinitions(
    CompanyApiClientFixture companyFix,
    CompanyGivenContext companyCtx,
    CompanyResultContext companyResultCtx)
{
    [When(@"eu atualizar a empresa cadastrada com a razão social ""(.*)""")]
    public async Task QuandoEuAtualizarAEmpresaCadastradaComARazaoSocial(string razaoSocial)
    {
        companyResultCtx.Empresa = await companyFix.UpdateAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            CompanyDomainDriver.CriarCompanyUpdateDtoValido(razaoSocial));
    }

    [When(@"eu atualizar a empresa cadastrada com o telefone ""(.*)""")]
    public async Task QuandoEuAtualizarAEmpresaCadastradaComOTelefone(string telefone)
    {
        companyResultCtx.Empresa = await companyFix.UpdateAsync(
            companyCtx.IdEmpresaCadastrada!.Value,
            CompanyDomainDriver.CriarCompanyUpdateDtoValido("Construtora Teste Ltda", telefone));
    }

    [When(@"eu atualizar uma empresa inexistente")]
    public async Task QuandoEuAtualizarUmaEmpresaInexistente()
    {
        await companyFix.UpdateAsync(
            TestConstants.ID_INEXISTENTE,
            CompanyDomainDriver.CriarCompanyUpdateDtoValido("Construtora Teste Ltda"));
    }

    [Then(@"a empresa retornada deve ter a razão social ""(.*)""")]
    public void EntaoAEmpresaRetornadaDeveTerARazaoSocial(string razaoSocial)
    {
        companyResultCtx.Empresa.Should().NotBeNull();
        companyResultCtx.Empresa!.CorporateName.Should().Be(razaoSocial);
    }

    [Then(@"a empresa retornada deve manter o CNPJ ""(.*)""")]
    public void EntaoAEmpresaRetornadaDeveManterOCnpj(string cnpj)
    {
        companyResultCtx.Empresa.Should().NotBeNull();
        companyResultCtx.Empresa!.Cnpj.Should().Be(cnpj);
    }
}
