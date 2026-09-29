using Jotanunes.Application.DTOs;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.Services;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Internal.BddTests.Drivers;

public class CompanyDomainDriver
{
    public static CompanyCreateDto CriarCompanyCreateDtoValido(
        string cnpj,
        string? razaoSocial = null,
        SupplierType tipoFornecimento = SupplierType.Material)
    {
        return new CompanyCreateDto
        {
            Cnpj = cnpj,
            CorporateName = razaoSocial ?? "Construtora Teste Ltda",
            TradeName = "Construtora Teste",
            Email = $"contato.{new string(cnpj.Where(char.IsDigit).ToArray())}@teste.com.br",
            Phone = "79999998888",
            ResponsibleName = "Responsável Teste",
            Address = new AddressDto
            {
                Street = "Rua Teste",
                Number = "100",
                Neighborhood = "Centro",
                City = "Aracaju",
                State = "SE",
                ZipCode = "49000000"
            },
            SupplierType = tipoFornecimento
        };
    }

    public static Company CriarEmpresaValida(
        string cnpj,
        string? razaoSocial = null,
        SupplierType tipoFornecimento = SupplierType.Material)
    {
        return CompanyFactory.Build(CriarCompanyCreateDtoValido(cnpj, razaoSocial, tipoFornecimento));
    }
}
