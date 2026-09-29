using Jotanunes.Domain.Entities;
using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;
using Jotanunes.Infra.Data.Context;

using Moq;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

[Binding]
internal class DocumentStepDefinitions(
    DatabaseFixture databaseFixture,
    ApiWebAppFactoryFixture apiWebAppFactory,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx,
    DocumentGivenContext documentCtx)
{
    private const string EmailUsuarioEnvio = "envio@teste.com.br";

    [Given(@"^que existe um documento de habilitação (pendente|aprovado|rejeitado) enviado pela empresa cadastrada$")]
    public async Task DadoQueExisteUmDocumentoDeHabilitacaoEnviadoPelaEmpresaCadastrada(string situacao)
    {
        await using var context = databaseFixture.CreateDbContext();

        var documento = await AdicionarDocumentoHabilitacaoAsync(context, TestConstants.TIPO_DOCUMENTO_CARTAO_CNPJ, situacao);
        await context.SaveChangesAsync();

        documentCtx.IdDocumentoCadastrado = documento.Id;
    }

    [Given(@"^que a empresa cadastrada tem todos os documentos de habilitação obrigatórios aprovados$")]
    public async Task DadoQueAEmpresaCadastradaTemTodosOsDocumentosDeHabilitacaoObrigatoriosAprovados()
    {
        await using var context = databaseFixture.CreateDbContext();

        foreach (var idTipoDocumento in TestConstants.TIPOS_DOCUMENTO_HABILITACAO_OBRIGATORIOS)
        {
            await AdicionarDocumentoHabilitacaoAsync(context, idTipoDocumento, "aprovado");
        }
        await context.SaveChangesAsync();
    }

    [Given(@"^que a empresa cadastrada tem todos os documentos de habilitação obrigatórios aprovados exceto um pendente$")]
    public async Task DadoQueAEmpresaCadastradaTemTodosOsDocumentosDeHabilitacaoObrigatoriosAprovadosExcetoUmPendente()
    {
        await using var context = databaseFixture.CreateDbContext();

        Document? pendente = null;
        foreach (var idTipoDocumento in TestConstants.TIPOS_DOCUMENTO_HABILITACAO_OBRIGATORIOS)
        {
            var situacao = idTipoDocumento == TestConstants.TIPO_DOCUMENTO_RG_CPF_SOCIOS ? "pendente" : "aprovado";
            var documento = await AdicionarDocumentoHabilitacaoAsync(context, idTipoDocumento, situacao);
            if (situacao == "pendente")
            {
                pendente = documento;
            }
        }
        await context.SaveChangesAsync();

        documentCtx.IdDocumentoCadastrado = pendente!.Id;
    }

    [Given(@"^que o arquivo do documento cadastrado está disponível no armazenamento$")]
    public void DadoQueOArquivoDoDocumentoCadastradoEstaDisponivelNoArmazenamento()
    {
        var conteudo = "%PDF-1.4 documento de teste"u8.ToArray();
        documentCtx.ConteudoArquivoArmazenado = conteudo;

        apiWebAppFactory.MockDocumentStorage
            .Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream(conteudo));
    }

    private async Task<Document> AdicionarDocumentoHabilitacaoAsync(ApplicationDbContext context, long idTipoDocumento, string situacao)
    {
        var idEmpresa = companyCtx.IdEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma empresa foi cadastrada - execute o Given 'que existe uma empresa cadastrada com CNPJ ...' antes.");

        var idUsuarioEnvio = await ObterUsuarioEnvioAsync(context, idEmpresa);

        var documento = DocumentDomainDriver.CriarDocumentoHabilitacao(idEmpresa, idTipoDocumento, idUsuarioEnvio);
        if (situacao == "aprovado")
        {
            documento.Approve();
        }
        else if (situacao == "rejeitado")
        {
            documento.Reject("Documento ilegível");
        }

        context.Documents.Add(documento);
        return documento;
    }

    private async Task<long> ObterUsuarioEnvioAsync(ApplicationDbContext context, long idEmpresa)
    {
        if (supplierUserCtx.IdUsuarioCadastrado.HasValue)
        {
            return supplierUserCtx.IdUsuarioCadastrado.Value;
        }

        var usuario = SupplierUserDomainDriver.CriarUsuarioValido(idEmpresa, EmailUsuarioEnvio, TestConstants.SENHA_PADRAO);
        context.SupplierUsers.Add(usuario);
        await context.SaveChangesAsync();

        supplierUserCtx.IdUsuarioCadastrado = usuario.Id;
        supplierUserCtx.EmailUsuarioCadastrado = usuario.Email;
        return usuario.Id;
    }
}
