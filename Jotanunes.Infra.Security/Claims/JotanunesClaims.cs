namespace Jotanunes.Infra.Security.Claims;

// Claims próprias do portal. CompanyId é o que garante o isolamento do RNF01:
// o fornecedor só enxerga dados da empresa presente no seu token.
public static class JotanunesClaims
{
    public const string CompanyId = "company_id";
    public const string MustChangePassword = "must_change_password";
    public const string SupplierRole = "Supplier";
}
