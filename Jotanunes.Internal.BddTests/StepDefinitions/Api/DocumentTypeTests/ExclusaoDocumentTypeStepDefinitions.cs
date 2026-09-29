using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTypeTests;

[Binding]
internal class ExclusaoDocumentTypeStepDefinitions(
    DocumentTypeApiClientFixture documentTypeFix,
    DocumentTypeGivenContext documentTypeCtx,
    DocumentTypeResultContext documentTypeResultCtx)
{
    [When(@"eu excluir o tipo de documento cadastrado")]
    public async Task QuandoEuExcluirOTipoDeDocumentoCadastrado()
    {
        documentTypeResultCtx.TipoDocumento = await documentTypeFix.DeleteAsync(documentTypeCtx.IdTipoDocumentoCadastrado!.Value);
    }

    [When(@"eu excluir um tipo de documento inexistente")]
    public async Task QuandoEuExcluirUmTipoDeDocumentoInexistente()
    {
        await documentTypeFix.DeleteAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"o tipo de documento retornado deve estar inativo")]
    public void EntaoOTipoDeDocumentoRetornadoDeveEstarInativo()
    {
        documentTypeResultCtx.TipoDocumento.Should().NotBeNull();
        documentTypeResultCtx.TipoDocumento!.Active.Should().BeFalse();
    }
}
