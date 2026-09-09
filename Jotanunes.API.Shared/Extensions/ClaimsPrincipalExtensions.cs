using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Jotanunes.API.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    public const string CompanyIdClaim = "company_id";

    public static long GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!long.TryParse(value, out var id))
        {
            throw new UnauthorizedAccessException("Token sem identificação do usuário.");
        }

        return id;
    }

    // Base para o isolamento por empresa exigido pelo RNF01: toda consulta da
    // frente externa deve ser filtrada por este valor.
    public static long GetCompanyId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(CompanyIdClaim)?.Value;

        if (!long.TryParse(value, out var id))
        {
            throw new UnauthorizedAccessException("Token sem identificação da empresa.");
        }

        return id;
    }
}
