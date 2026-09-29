using AwesomeAssertions;

using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Api.DocumentTests;

[Binding]
internal class DownloadDocumentStepDefinitions(
    DocumentApiClientFixture documentFix,
    DocumentGivenContext documentCtx)
{
    private byte[]? _arquivo;

    [When(@"eu baixar o arquivo do documento cadastrado")]
    public async Task QuandoEuBaixarOArquivoDoDocumentoCadastrado()
    {
        _arquivo = await documentFix.DownloadAsync(documentCtx.IdDocumentoCadastrado!.Value);
    }

    [When(@"eu baixar o arquivo de um documento inexistente")]
    public async Task QuandoEuBaixarOArquivoDeUmDocumentoInexistente()
    {
        await documentFix.DownloadAsync(TestConstants.ID_INEXISTENTE);
    }

    [Then(@"o arquivo recebido deve ser o arquivo armazenado")]
    public void EntaoOArquivoRecebidoDeveSerOArquivoArmazenado()
    {
        _arquivo.Should().Equal(documentCtx.ConteudoArquivoArmazenado);
    }
}
