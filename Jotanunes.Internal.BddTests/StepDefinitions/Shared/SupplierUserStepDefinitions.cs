using Jotanunes.Internal.BddTests.Drivers;
using Jotanunes.Internal.BddTests.Support;
using Jotanunes.Internal.BddTests.Support.Contexts;
using Jotanunes.Internal.BddTests.Support.Fixtures;

namespace Jotanunes.Internal.BddTests.StepDefinitions.Shared;

[Binding]
internal class SupplierUserStepDefinitions(
    DatabaseFixture databaseFixture,
    CompanyGivenContext companyCtx,
    SupplierUserGivenContext supplierUserCtx)
{
    [Given(@"^que existe um usuário de acesso ""(.*)"" (ativo|inativo) para a empresa cadastrada$")]
    public async Task DadoQueExisteUmUsuarioDeAcessoParaAEmpresaCadastrada(string email, string situacao)
    {
        var idEmpresa = companyCtx.IdEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma empresa foi cadastrada - execute o Given 'que existe uma empresa cadastrada com CNPJ ...' antes.");

        await using var context = databaseFixture.CreateDbContext();

        var usuario = SupplierUserDomainDriver.CriarUsuarioValido(idEmpresa, email, TestConstants.SENHA_PADRAO, ativo: situacao == "ativo");
        context.SupplierUsers.Add(usuario);
        await context.SaveChangesAsync();

        supplierUserCtx.IdUsuarioCadastrado = usuario.Id;
        supplierUserCtx.EmailUsuarioCadastrado = usuario.Email;
    }

    [Given(@"^que existe um usuário de acesso ""(.*)"" para a outra empresa$")]
    public async Task DadoQueExisteUmUsuarioDeAcessoParaAOutraEmpresa(string email)
    {
        var idOutraEmpresa = companyCtx.IdOutraEmpresaCadastrada
            ?? throw new InvalidOperationException(
                "Nenhuma outra empresa foi cadastrada - execute o Given 'que existe outra empresa cadastrada' antes.");

        await using var context = databaseFixture.CreateDbContext();

        var usuario = SupplierUserDomainDriver.CriarUsuarioValido(idOutraEmpresa, email, TestConstants.SENHA_PADRAO);
        context.SupplierUsers.Add(usuario);
        await context.SaveChangesAsync();

        supplierUserCtx.IdUsuarioOutraEmpresa = usuario.Id;
    }
}
