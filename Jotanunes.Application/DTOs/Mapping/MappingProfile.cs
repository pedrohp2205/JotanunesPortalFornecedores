using AutoMapper;
using Jotanunes.Application.DTOs.Auth;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.Users;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.DTOs.Mapping;

// O mapeamento é usado apenas no sentido entidade -> DTO (leitura).
// A escrita passa pelos construtores e métodos do domínio, para que as
// regras de negócio das entidades não sejam contornadas pelo mapper.
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Address, AddressDto>();

        CreateMap<Company, CompanyDto>()
            .ForMember(dest => dest.FormattedCnpj, opt => opt.MapFrom(src => Cnpj.Format(src.Cnpj)))
            .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<SupplierUser, SupplierUserDto>();

        CreateMap<SupplierUser, AuthenticatedUserDto>()
            .ForMember(dest => dest.CompanyCorporateName, opt => opt.MapFrom(src => src.Company.CorporateName))
            .ForMember(dest => dest.CompanyCnpj, opt => opt.MapFrom(src => src.Company.Cnpj));
    }
}
