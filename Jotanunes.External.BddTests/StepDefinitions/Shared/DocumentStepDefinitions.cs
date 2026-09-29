using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;

using Moq;

namespace Jotanunes.External.BddTests.StepDefinitions.Shared;

[Binding]
internal class DocumentStepDefinitions(
    DatabaseFixture databaseFixture,
    ApiWebAppFactoryFixture apiWebAppFactory,
    SupplierUserStepDefinitions supplierUserSteps,
    CompanyGivenContext companyCtx,
    DocumentGivenContext documentCtx)
{
    [Given(@"^que existe um documento de habilitação (pendente|aprovado) enviado pela empresa cadastrada$")]
    public async Task DadoQueExisteUmDocumentoDeHabilitacaoEnviadoPelaEmpresaCadastrada(string situacao)
    {
        var idEmpresa = companyCtx.IdEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma empresa foi cadastrada - execute o Given 'que existe uma empresa cadastrada com CNPJ ...' antes.");
        var idUsuarioEnvio = await supplierUserSteps.ObterIdUsuarioEnvioAsync();

        documentCtx.IdDocumentoCadastrado = await CadastrarDocumentoAsync(idEmpresa, idUsuarioEnvio, aprovado: situacao == "aprovado");
    }

    [Given(@"^que existe um documento de habilitação enviado pela outra empresa$")]
    public async Task DadoQueExisteUmDocumentoDeHabilitacaoEnviadoPelaOutraEmpresa()
    {
        var idOutraEmpresa = companyCtx.IdOutraEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma outra empresa foi cadastrada - execute o Given 'que existe outra empresa cadastrada' antes.");

        long idUsuarioEnvio;
        await using (var context = databaseFixture.CreateDbContext())
        {
            var usuario = SupplierUserDomainDriver.CriarUsuarioValido(idOutraEmpresa, "envio@outra.com.br", TestConstants.SENHA_PADRAO);
            context.SupplierUsers.Add(usuario);
            await context.SaveChangesAsync();
            idUsuarioEnvio = usuario.Id;
        }

        documentCtx.IdDocumentoOutraEmpresa = await CadastrarDocumentoAsync(idOutraEmpresa, idUsuarioEnvio, aprovado: false);
    }

    [Given(@"^que o arquivo do documento está disponível no armazenamento$")]
    public void DadoQueOArquivoDoDocumentoEstaDisponivelNoArmazenamento()
    {
        var conteudo = DocumentUploadDriver.ArquivoPdf;
        documentCtx.ConteudoArquivoArmazenado = conteudo;

        apiWebAppFactory.MockDocumentStorage
            .Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(conteudo));
    }

    private async Task<long> CadastrarDocumentoAsync(long idEmpresa, long idUsuarioEnvio, bool aprovado)
    {
        await using var context = databaseFixture.CreateDbContext();

        var documento = DocumentDomainDriver.CriarDocumentoHabilitacao(idEmpresa, TestConstants.TIPO_DOCUMENTO_CARTAO_CNPJ, idUsuarioEnvio);
        if (aprovado)
        {
            documento.Approve();
        }

        context.Documents.Add(documento);
        await context.SaveChangesAsync();

        return documento.Id;
    }
}
