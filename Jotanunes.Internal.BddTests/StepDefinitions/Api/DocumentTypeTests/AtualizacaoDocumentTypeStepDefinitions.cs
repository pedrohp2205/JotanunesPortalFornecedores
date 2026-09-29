using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTypeTests;

[Binding]
internal class AtualizacaoDocumentTypeStepDefinitions(
    DocumentTypeApiClientFixture documentTypeFix,
    DocumentTypeGivenContext documentTypeCtx,
    DocumentTypeResultContext documentTypeResultCtx)
{
    [When(@"eu atualizar o tipo de documento cadastrado com o nome ""(.*)""")]
    public async Task QuandoEuAtualizarOTipoDeDocumentoCadastradoComONome(string nome)
    {
        documentTypeResultCtx.TipoDocumento = await documentTypeFix.UpdateAsync(
            documentTypeCtx.IdTipoDocumentoCadastrado!.Value,
            DocumentTypeDomainDriver.CriarDocumentTypeUpdateDtoValido(nome));
    }

    [When(@"eu tornar o tipo de documento cadastrado condicional sem descrever a condição")]
    public async Task QuandoEuTornarOTipoDeDocumentoCadastradoCondicionalSemDescreverACondicao()
    {
        documentTypeResultCtx.TipoDocumento = await documentTypeFix.UpdateAsync(
            documentTypeCtx.IdTipoDocumentoCadastrado!.Value,
            DocumentTypeDomainDriver.CriarDocumentTypeUpdateDtoValido("Alvará", condicional: true));
    }

    [When(@"eu atualizar um tipo de documento inexistente")]
    public async Task QuandoEuAtualizarUmTipoDeDocumentoInexistente()
    {
        await documentTypeFix.UpdateAsync(TestConstants.ID_INEXISTENTE, DocumentTypeDomainDriver.CriarDocumentTypeUpdateDtoValido("Alvará"));
    }

    [Then(@"o tipo de documento retornado deve ter o nome ""(.*)"" e manter o código ""(.*)""")]
    public void EntaoOTipoDeDocumentoRetornadoDeveTerONomeEManterOCodigo(string nome, string codigo)
    {
        documentTypeResultCtx.TipoDocumento.Should().NotBeNull();
        documentTypeResultCtx.TipoDocumento!.Name.Should().Be(nome);
        documentTypeResultCtx.TipoDocumento.Code.Should().Be(codigo);
    }
}
