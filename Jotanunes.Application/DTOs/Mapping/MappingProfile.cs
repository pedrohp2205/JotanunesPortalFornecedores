using AutoMapper;
using Jotanunes.Application.DTOs.Auth;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Application.DTOs.Users;
using Jotanunes.Application.DTOs.WorkSites;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.DTOs.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Address, AddressDto>();

        CreateMap<Company, CompanyDto>()
            .ForMember(dest => dest.FormattedCnpj, opt => opt.MapFrom(src => Cnpj.Format(src.Cnpj)))
            .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.SupplierTypeDescription, opt => opt.MapFrom(src => src.SupplierType.ToString()));

        CreateMap<SupplierUser, SupplierUserDto>();

        CreateMap<SupplierUser, AuthenticatedUserDto>()
            .ForMember(dest => dest.CompanyCorporateName, opt => opt.MapFrom(src => src.Company.CorporateName))
            .ForMember(dest => dest.CompanyCnpj, opt => opt.MapFrom(src => src.Company.Cnpj));

        CreateMap<WorkSite, WorkSiteDto>();

        CreateMap<DocumentType, DocumentTypeDto>()
            .ForMember(dest => dest.CategoryDescription, opt => opt.MapFrom(src => src.Category.ToString()))
            .ForMember(dest => dest.AppliesToDescription, opt => opt.MapFrom(src => src.AppliesTo.ToString()))
            .ForMember(dest => dest.SubjectDescription, opt => opt.MapFrom(src => src.Subject.ToString()));
    }
}
