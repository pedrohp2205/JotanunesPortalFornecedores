using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Domain.Entities;

// Objeto de valor com o endereço da empresa fornecedora.
// Persistido como owned type, ou seja, colunas na própria tabela de empresas.
public class Address
{
    public string Street { get; private set; } = string.Empty;
    public string Number { get; private set; } = string.Empty;
    public string? Complement { get; private set; }
    public string Neighborhood { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;

    protected Address() { }

    public Address(
        string street,
        string number,
        string neighborhood,
        string city,
        string state,
        string zipCode,
        string? complement = null)
    {
        zipCode = DigitsOnly(zipCode);
        Validate(street, number, neighborhood, city, state, zipCode);

        Street = street.Trim();
        Number = number.Trim();
        Neighborhood = neighborhood.Trim();
        City = city.Trim();
        State = state.Trim().ToUpperInvariant();
        ZipCode = zipCode;
        Complement = string.IsNullOrWhiteSpace(complement) ? null : complement.Trim();
    }

    private static void Validate(string street, string number, string neighborhood, string city, string state, string zipCode)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(street), "Logradouro não pode ser vazio.");
        JotanunesException.When(string.IsNullOrWhiteSpace(number), "Número não pode ser vazio.");
        JotanunesException.When(string.IsNullOrWhiteSpace(neighborhood), "Bairro não pode ser vazio.");
        JotanunesException.When(string.IsNullOrWhiteSpace(city), "Cidade não pode ser vazia.");
        JotanunesException.When(string.IsNullOrWhiteSpace(state), "UF não pode ser vazia.");
        JotanunesException.When(state.Trim().Length != 2, "UF deve ter 2 caracteres.");
        JotanunesException.When(string.IsNullOrWhiteSpace(zipCode), "CEP não pode ser vazio.");
        JotanunesException.When(zipCode.Length != 8, "CEP deve ter 8 dígitos.");
    }

    private static string DigitsOnly(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());
    }
}
