using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Domain.Entities;

// Empresa fornecedora prestadora de serviço para a Jotanunes.
public class Company : BaseEntity
{
    public string Cnpj { get; private set; } = string.Empty;
    public string CorporateName { get; private set; } = string.Empty;
    public string TradeName { get; private set; } = string.Empty;
    public string? StateRegistration { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string ResponsibleName { get; private set; } = string.Empty;
    public CompanyStatus Status { get; private set; }
    public Address Address { get; private set; } = null!;

    protected Company() { }

    public Company(
        string cnpj,
        string corporateName,
        string tradeName,
        string email,
        string phone,
        string responsibleName,
        Address address,
        string? stateRegistration = null)
    {
        cnpj = Validation.Cnpj.Normalize(cnpj);
        phone = DigitsOnly(phone);
        Validate(cnpj, corporateName, tradeName, email, phone, responsibleName, address);

        Cnpj = cnpj;
        CorporateName = corporateName.Trim();
        TradeName = tradeName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone;
        ResponsibleName = responsibleName.Trim();
        StateRegistration = string.IsNullOrWhiteSpace(stateRegistration) ? null : stateRegistration.Trim();
        Address = address;
        Status = CompanyStatus.PendingDocumentation;
    }

    public void Update(
        string corporateName,
        string tradeName,
        string email,
        string phone,
        string responsibleName,
        Address address,
        string? stateRegistration = null)
    {
        phone = DigitsOnly(phone);
        Validate(Cnpj, corporateName, tradeName, email, phone, responsibleName, address);

        CorporateName = corporateName.Trim();
        TradeName = tradeName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone;
        ResponsibleName = responsibleName.Trim();
        StateRegistration = string.IsNullOrWhiteSpace(stateRegistration) ? null : stateRegistration.Trim();
        Address = address;
    }

    private static void Validate(
        string cnpj,
        string corporateName,
        string tradeName,
        string email,
        string phone,
        string responsibleName,
        Address address)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(cnpj), "CNPJ não pode ser vazio.");
        JotanunesException.When(!Validation.Cnpj.IsValid(cnpj), "CNPJ inválido.");

        JotanunesException.When(string.IsNullOrWhiteSpace(corporateName), "Razão social não pode ser vazia.");
        JotanunesException.When(corporateName.Trim().Length < 3, "Razão social deve ter ao menos 3 caracteres.");

        JotanunesException.When(string.IsNullOrWhiteSpace(tradeName), "Nome fantasia não pode ser vazio.");

        JotanunesException.When(string.IsNullOrWhiteSpace(email), "E-mail não pode ser vazio.");
        JotanunesException.When(!email.Contains('@') || !email.Contains('.'), "E-mail deve conter '@' e '.'");

        JotanunesException.When(string.IsNullOrWhiteSpace(phone), "Telefone não pode ser vazio.");
        JotanunesException.When(phone.Length is < 10 or > 11, "Telefone deve ter 10 ou 11 dígitos (com DDD).");

        JotanunesException.When(string.IsNullOrWhiteSpace(responsibleName), "Nome do responsável não pode ser vazio.");

        JotanunesException.When(address is null, "Endereço é obrigatório.");
    }

    private static string DigitsOnly(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());
    }
}
