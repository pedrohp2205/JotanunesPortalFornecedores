using AwesomeAssertions;

using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Domain.Enums;
using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.CompanyTests;

[Binding]
internal class CadastroCompanyStepDefinitions(CompanyApiClientFixture companyFix)
{
    private CompanyDto? _empresa;

    [When(@"eu cadastrar a empresa com CNPJ ""(.*)""")]
    public async Task QuandoEuCadastrarAEmpresaComCnpj(string cnpj)
    {
        _empresa = await companyFix.CreateAsync(CompanyDomainDriver.CriarCompanyCreateDtoValido(cnpj));
    }

    [Then(@"a empresa cadastrada deve ter o CNPJ ""(.*)""")]
    public void EntaoAEmpresaCadastradaDeveTerOCnpj(string cnpj)
    {
        _empresa.Should().NotBeNull();
        _empresa!.Id.Should().BePositive();
        _empresa.Cnpj.Should().Be(cnpj);
    }

    [Then(@"a empresa cadastrada deve estar aguardando documentação")]
    public void EntaoAEmpresaCadastradaDeveEstarAguardandoDocumentacao()
    {
        _empresa.Should().NotBeNull();
        _empresa!.Status.Should().Be(CompanyStatus.PendingDocumentation);
    }
}
