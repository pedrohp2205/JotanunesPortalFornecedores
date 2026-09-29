using AwesomeAssertions;

using Jotanunes.Application.DTOs.Compliance;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTests;

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

    [When(@"eu consultar o checklist de uma solicitação inexistente")]
    public async Task QuandoEuConsultarOChecklistDeUmaSolicitacaoInexistente()
    {
        await documentFix.GetChecklistAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"^o checklist deve listar (\d+) documentos de habilitação sem nenhum atendido$")]
    public void EntaoOChecklistDeveListarDocumentosDeHabilitacaoSemNenhumAtendido(int quantidade)
    {
        _checklist.Should().NotBeNull();
        _checklist!.SupplyRequestId.Should().Be(supplyRequestCtx.IdSolicitacaoCadastrada);
        _checklist.OnboardingItems.Should().HaveCount(quantidade);
        _checklist.OnboardingItems.Should().OnlyContain(i => !i.IsSatisfied && i.Status == ChecklistItemStatus.NotSent);
    }

    [Then(@"o item Cartão de CNPJ do checklist deve estar aprovado")]
    public void EntaoOItemCartaoDeCnpjDoChecklistDeveEstarAprovado()
    {
        _checklist.Should().NotBeNull();
        var item = _checklist!.OnboardingItems.Single(i => i.DocumentTypeId == TestConstants.TIPO_DOCUMENTO_CARTAO_CNPJ);
        item.IsSatisfied.Should().BeTrue();
        item.Status.Should().Be(ChecklistItemStatus.Approved);
    }
}
