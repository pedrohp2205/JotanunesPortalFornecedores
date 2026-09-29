using AwesomeAssertions;

using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.External.BddTests.Support;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.DocumentTests;

[Binding]
internal class ChecklistDocumentStepDefinitions(
    DocumentApiClientFixture documentFix,
    SupplyRequestGivenContext supplyRequestCtx)
{
    private ComplianceChecklistDto? _checklist;

    [When(@"eu consultar o checklist da solicitação cadastrada")]
    public async Task QuandoEuConsultarOChecklistDaSolicitacaoCadastrada()
    {
        _checklist = await documentFix.GetChecklistAsync(supplyRequestCtx.IdSolicitacaoCadastrada!.Value);
    }

    [When(@"eu consultar o checklist da solicitação da outra empresa")]
    public async Task QuandoEuConsultarOChecklistDaSolicitacaoDaOutraEmpresa()
    {
        await documentFix.GetChecklistAsync(supplyRequestCtx.IdSolicitacaoOutraEmpresa!.Value);
    }

    [Then(@"o checklist deve ser da solicitação cadastrada com o Cartão de CNPJ aprovado")]
    public void EntaoOChecklistDeveSerDaSolicitacaoCadastradaComOCartaoDeCnpjAprovado()
    {
        _checklist.Should().NotBeNull();
        _checklist!.SupplyRequestId.Should().Be(supplyRequestCtx.IdSolicitacaoCadastrada);

        var item = _checklist.OnboardingItems.Single(i => i.DocumentTypeId == TestConstants.TIPO_DOCUMENTO_CARTAO_CNPJ);
        item.Status.Should().Be(ChecklistItemStatus.Approved);
        item.IsSatisfied.Should().BeTrue();
    }
}
