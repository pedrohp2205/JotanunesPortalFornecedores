using Jotanunes.External.BddTests.Drivers;
using Jotanunes.External.BddTests.Support;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

namespace Jotanunes.External.BddTests.StepDefinitions.Shared;

[Binding]
internal class SupplierUserStepDefinitions(
    DatabaseFixture databaseFixture,
    ApiClientFixture apiClientFix,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx)
{
    private const string EmailFornecedorPadrao = "fornecedor@teste.com.br";

    [Given(@"^que existe um fornecedor ""(.*)"" com senha (provisória|definitiva) para a empresa cadastrada$")]
    public async Task DadoQueExisteUmFornecedorParaAEmpresaCadastrada(string email, string tipoSenha)
    {
        await CadastrarFornecedorAsync(email, deveTrocarSenha: tipoSenha == "provisória");
    }

    [Given("que eu não estou autenticado")]
    public void DadoQueEuNaoEstouAutenticado()
    {
        apiClientFix.ResetAuthorization();
    }

    [Given(@"que eu estou autenticado como o fornecedor ""(.*)""")]
    public async Task DadoQueEuEstouAutenticadoComoOFornecedor(string email)
    {
        if (!string.Equals(supplierUserCtx.EmailAtual, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"O fornecedor '{email}' não foi cadastrado no cenário - execute o Given 'que existe um fornecedor \"{email}\" ...' antes.");
        }

        await apiClientFix.LoginAsSupplier(email, supplierUserCtx.SenhaAtual!);
    }

    [Given(@"^que eu estou autenticado com senha (provisória|definitiva)$")]
    public async Task DadoQueEuEstouAutenticadoComSenha(string tipoSenha)
    {
        await CadastrarFornecedorAsync(EmailFornecedorPadrao, deveTrocarSenha: tipoSenha == "provisória");
        await apiClientFix.LoginAsSupplier(EmailFornecedorPadrao, supplierUserCtx.SenhaAtual!);
    }

    private async Task CadastrarFornecedorAsync(string email, bool deveTrocarSenha)
    {
        var idEmpresa = companyCtx.IdEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma empresa foi cadastrada - execute o Given 'que existe uma empresa cadastrada com CNPJ ...' antes.");

        await using var context = databaseFixture.CreateDbContext();

        var usuario = SupplierUserDomainDriver.CriarUsuarioValido(idEmpresa, email, TestConstants.SENHA_PADRAO, deveTrocarSenha: deveTrocarSenha);
        context.SupplierUsers.Add(usuario);
        await context.SaveChangesAsync();

        supplierUserCtx.IdUsuarioCadastrado = usuario.Id;
        supplierUserCtx.EmailAtual = usuario.Email;
        supplierUserCtx.SenhaAtual = TestConstants.SENHA_PADRAO;
    }
}
