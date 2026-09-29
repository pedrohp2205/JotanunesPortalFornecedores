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
            Address = CriarAddressDtoValido(),
            SupplierType = tipoFornecimento
        };
    }

    public static CompanyUpdateDto CriarCompanyUpdateDtoValido(string razaoSocial, string telefone = "79988887777")
    {
        return new CompanyUpdateDto
        {
            CorporateName = razaoSocial,
            TradeName = "Construtora Atualizada",
            Email = "atualizado@teste.com.br",
            Phone = telefone,
            ResponsibleName = "Responsável Atualizado",
            Address = CriarAddressDtoValido()
        };
    }

    public static AddressDto CriarAddressDtoValido()
    {
        return new AddressDto
        {
            Street = "Rua Teste",
            Number = "100",
            Neighborhood = "Centro",
            City = "Aracaju",
            State = "SE",
            ZipCode = "49000000"
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
