using Jotanunes.Application.DTOs;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Application.Services;

public static class CompanyFactory
{
    public static Company Build(CompanyCreateDto model)
    {
        return new Company(
            model.Cnpj,
            model.CorporateName,
            model.TradeName,
            model.Email,
            model.Phone,
            model.ResponsibleName,
            BuildAddress(model.Address),
            model.SupplierType,
            model.StateRegistration);
    }

    public static Address BuildAddress(AddressDto model)
    {
        JotanunesException.When(model is null, "Endereço é obrigatório.");

        return new Address(
            model!.Street,
            model.Number,
            model.Neighborhood,
            model.City,
            model.State,
            model.ZipCode,
            model.Complement);
    }
}
