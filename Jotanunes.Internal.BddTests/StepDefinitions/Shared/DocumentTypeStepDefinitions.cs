using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

[Binding]
internal class DocumentTypeStepDefinitions(
    DatabaseFixture databaseFixture,
    DocumentTypeGivenContext documentTypeCtx)
{
    [Given(@"^que existe um tipo de documento cadastrado com código ""(.*)""$")]
    public async Task DadoQueExisteUmTipoDeDocumentoCadastradoComCodigo(string codigo)
    {
        await using var context = databaseFixture.CreateDbContext();

        var tipoDocumento = DocumentTypeDomainDriver.CriarTipoDocumentoValido(codigo);
        context.DocumentTypes.Add(tipoDocumento);
        await context.SaveChangesAsync();

        documentTypeCtx.IdTipoDocumentoCadastrado = tipoDocumento.Id;
    }
}
