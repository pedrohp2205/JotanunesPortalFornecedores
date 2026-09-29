using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTypeTests;

[Binding]
internal class CadastroDocumentTypeStepDefinitions(
    DocumentTypeApiClientFixture documentTypeFix,
    DocumentTypeResultContext documentTypeResultCtx)
{
    [When(@"eu cadastrar o tipo de documento com código ""(.*)""")]
    public async Task QuandoEuCadastrarOTipoDeDocumentoComCodigo(string codigo)
    {
        documentTypeResultCtx.TipoDocumento = await documentTypeFix.CreateAsync(DocumentTypeDomainDriver.CriarDocumentTypeCreateDtoValido(codigo));
    }

    [When(@"eu cadastrar o tipo de documento condicional com código ""(.*)"" sem descrever a condição")]
    public async Task QuandoEuCadastrarOTipoDeDocumentoCondicionalSemDescreverACondicao(string codigo)
    {
        var dto = DocumentTypeDomainDriver.CriarDocumentTypeCreateDtoValido(codigo);
        dto.IsConditional = true;
        dto.ConditionDescription = null;

        documentTypeResultCtx.TipoDocumento = await documentTypeFix.CreateAsync(dto);
    }
}
