using AwesomeAssertions;

using Jotanunes.Application.DTOs.Auth;
using Jotanunes.External.BddTests.Support.Contexts;
using Jotanunes.External.BddTests.Support.Fixtures;
using Jotanunes.External.BddTests.Support.Fixtures.ApiClients;

using Microsoft.EntityFrameworkCore;

namespace Jotanunes.External.BddTests.StepDefinitions.Api.SupplierUserTests;

[Binding]
internal class EsqueciSenhaSupplierUserStepDefinitions(
    AuthApiClientFixture authFix,
    DatabaseFixture databaseFixture,
    SupplierUserGivenContext supplierUserCtx)
{
    [When(@"eu solicitar a redefinição de senha para o e-mail ""(.*)""")]
    public async Task QuandoEuSolicitarARedefinicaoDeSenhaParaOEmail(string email)
    {
        await authFix.ForgotPasswordAsync(new ForgotPasswordDto { Email = email });
    }

    [Then(@"o fornecedor cadastrado deve ter um token de redefinição de senha válido")]
    public async Task EntaoOFornecedorCadastradoDeveTerUmTokenDeRedefinicaoDeSenhaValido()
    {
        await using var context = databaseFixture.CreateDbContext();
        var usuario = await context.SupplierUsers.AsNoTracking().SingleAsync(u => u.Id == supplierUserCtx.IdUsuarioCadastrado);

        usuario.PasswordResetTokenHash.Should().NotBeNullOrWhiteSpace();
        usuario.PasswordResetExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }
}
