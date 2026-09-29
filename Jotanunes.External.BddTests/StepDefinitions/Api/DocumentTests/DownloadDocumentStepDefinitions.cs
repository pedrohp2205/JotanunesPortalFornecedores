using AwesomeAssertions;

using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.DocumentTests;

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

    [When(@"eu baixar o arquivo do documento da outra empresa")]
    public async Task QuandoEuBaixarOArquivoDoDocumentoDaOutraEmpresa()
    {
        await documentFix.DownloadAsync(documentCtx.IdDocumentoOutraEmpresa!.Value);
    }

    [Then(@"o arquivo recebido deve ser o arquivo armazenado")]
    public void EntaoOArquivoRecebidoDeveSerOArquivoArmazenado()
    {
        _arquivo.Should().Equal(documentCtx.ConteudoArquivoArmazenado);
    }
}
