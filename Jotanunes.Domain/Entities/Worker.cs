using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Domain.Entities;

public class Worker : BaseEntity
{
    public long CompanyId { get; private set; }
    public Company Company { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;
    public bool Active { get; private set; }

    protected Worker() { }

    public Worker(long companyId, string name, string cpf)
    {
        cpf = Validation.Cpf.Normalize(cpf);
        Validate(name);
        JotanunesException.When(string.IsNullOrWhiteSpace(cpf), "CPF do trabalhador não pode ser vazio.");
        JotanunesException.When(!Validation.Cpf.IsValid(cpf), "CPF do trabalhador inválido.");

        CompanyId = companyId;
        Name = name.Trim();
        Cpf = cpf;
        Active = true;
    }

    public void Update(string name)
    {
        Validate(name);

        Name = name.Trim();
    }

    public void Activate()
    {
        Active = true;
    }

    public void Deactivate()
    {
        Active = false;
    }

    private static void Validate(string name)
    {
        JotanunesException.When(string.IsNullOrWhiteSpace(name), "Nome do trabalhador não pode ser vazio.");
    }
}
