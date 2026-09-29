using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.External.BddTests.StepDefinitions.Shared;

[Binding]
internal class SupplierUserStepDefinitions(
    DatabaseFixture databaseFixture,
    ApiClientFixture apiClientFix,
    CompanyStepDefinitions companySteps,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx)
{
    [Given(@"^que existe um fornecedor ""(.*)"" com senha (provisória|definitiva) para a empresa cadastrada$")]
    public async Task DadoQueExisteUmFornecedorParaAEmpresaCadastrada(string email, string tipoSenha)
    {
        await CadastrarFornecedorAsync(ObterIdEmpresaCadastrada(), email, deveTrocarSenha: tipoSenha == "provisória");
    }

    [Given(@"^que existe um fornecedor ""(.*)"" inativo para a empresa cadastrada$")]
    public async Task DadoQueExisteUmFornecedorInativoParaAEmpresaCadastrada(string email)
    {
        await CadastrarFornecedorAsync(ObterIdEmpresaCadastrada(), email, deveTrocarSenha: false, ativo: false);
    }

    [Given(@"^que o fornecedor cadastrado solicitou a redefinição de senha e recebeu o token ""(.*)""$")]
    public async Task DadoQueOFornecedorCadastradoSolicitouARedefinicaoDeSenha(string token)
    {
        await using var context = databaseFixture.CreateDbContext();

        var usuario = await context.SupplierUsers.SingleAsync(u => u.Id == supplierUserCtx.IdUsuarioCadastrado);
        usuario.AssignPasswordResetToken(SupplierUserDomainDriver.HashToken(token));
        await context.SaveChangesAsync();
    }

    [Given(@"^que eu não estou autenticado$")]
    public void DadoQueEuNaoEstouAutenticado()
    {
        apiClientFix.ResetAuthorization();
    }

    [Given(@"^que eu estou autenticado como o fornecedor ""(.*)""$")]
    public async Task DadoQueEuEstouAutenticadoComoOFornecedor(string email)
    {
        if (!string.Equals(supplierUserCtx.EmailAtual, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"O fornecedor '{email}' não foi cadastrado no cenário - execute o Given 'que existe um fornecedor \"{email}\" ...' antes.");
        }

        await EntrarAsync(email);
    }

    [Given(@"^que eu estou autenticado com senha (provisória|definitiva)$")]
    public async Task DadoQueEuEstouAutenticadoComSenha(string tipoSenha)
    {
        await CadastrarFornecedorAsync(ObterIdEmpresaCadastrada(), TestConstants.EMAIL_FORNECEDOR_PADRAO, deveTrocarSenha: tipoSenha == "provisória");
        await EntrarAsync(TestConstants.EMAIL_FORNECEDOR_PADRAO);
    }

    [Given(@"^que eu estou autenticado como fornecedor de outra empresa$")]
    public async Task DadoQueEuEstouAutenticadoComoFornecedorDeOutraEmpresa()
    {
        if (!companyCtx.IdOutraEmpresaCadastrada.HasValue)
        {
            await companySteps.DadoQueExisteOutraEmpresaCadastrada();
        }

        await using (var context = databaseFixture.CreateDbContext())
        {
            var usuario = SupplierUserDomainDriver.CriarUsuarioValido(
                companyCtx.IdOutraEmpresaCadastrada!.Value, TestConstants.EMAIL_FORNECEDOR_OUTRA_EMPRESA, TestConstants.SENHA_PADRAO);
            context.SupplierUsers.Add(usuario);
            await context.SaveChangesAsync();
        }

        await apiClientFix.LoginAsSupplier(TestConstants.EMAIL_FORNECEDOR_OUTRA_EMPRESA, TestConstants.SENHA_PADRAO);
    }

    public async Task<long> ObterIdUsuarioEnvioAsync()
    {
        if (!supplierUserCtx.IdUsuarioCadastrado.HasValue)
        {
            await CadastrarFornecedorAsync(ObterIdEmpresaCadastrada(), "envio@teste.com.br", deveTrocarSenha: false);
        }

        return supplierUserCtx.IdUsuarioCadastrado!.Value;
    }

    private async Task EntrarAsync(string email)
    {
        var token = await apiClientFix.LoginAsSupplier(email, supplierUserCtx.SenhaAtual!);
        supplierUserCtx.RefreshTokenAtual = token.RefreshToken;
    }

    private async Task CadastrarFornecedorAsync(long idEmpresa, string email, bool deveTrocarSenha, bool ativo = true)
    {
        await using var context = databaseFixture.CreateDbContext();

        var usuario = SupplierUserDomainDriver.CriarUsuarioValido(idEmpresa, email, TestConstants.SENHA_PADRAO, deveTrocarSenha: deveTrocarSenha, ativo: ativo);
        context.SupplierUsers.Add(usuario);
        await context.SaveChangesAsync();

        supplierUserCtx.IdUsuarioCadastrado = usuario.Id;
        supplierUserCtx.EmailAtual = usuario.Email;
        supplierUserCtx.SenhaAtual = TestConstants.SENHA_PADRAO;
    }

    private long ObterIdEmpresaCadastrada()
    {
        return companyCtx.IdEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma empresa foi cadastrada - execute o Given 'que existe uma empresa cadastrada com CNPJ ...' antes.");
    }
}
