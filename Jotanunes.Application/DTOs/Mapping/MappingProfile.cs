using AutoMapper;
using Jotanunes.Application.DTOs.Auth;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Application.DTOs.SupplyRequests;
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

        CreateMap<SupplyRequest, SupplyRequestDto>()
            .ForMember(dest => dest.CompanyCorporateName, opt => opt.MapFrom(src => src.Company.CorporateName))
            .ForMember(dest => dest.CompanyTradeName, opt => opt.MapFrom(src => src.Company.TradeName))
            .ForMember(dest => dest.CompanyFormattedCnpj, opt => opt.MapFrom(src => Cnpj.Format(src.Company.Cnpj)))
            .ForMember(dest => dest.WorkSiteName, opt => opt.MapFrom(src => src.WorkSite.Name))
            .ForMember(dest => dest.RenewalPeriodDays, opt => opt.MapFrom(src => src.WorkSite.RenewalPeriodDays))
            .ForMember(dest => dest.SupplierTypeDescription, opt => opt.MapFrom(src => src.SupplierType.ToString()))
            .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<DocumentType, SupplierDocumentTypeDto>()
            .ForMember(dest => dest.CategoryDescription, opt => opt.MapFrom(src => src.Category.ToString()))
            .ForMember(dest => dest.AppliesToDescription, opt => opt.MapFrom(src => src.AppliesTo.ToString()))
            .ForMember(dest => dest.SubjectDescription, opt => opt.MapFrom(src => src.Subject.ToString()));

        CreateMap<DocumentType, DocumentTypeDto>()
            .ForMember(dest => dest.CategoryDescription, opt => opt.MapFrom(src => src.Category.ToString()))
            .ForMember(dest => dest.AppliesToDescription, opt => opt.MapFrom(src => src.AppliesTo.ToString()))
            .ForMember(dest => dest.SubjectDescription, opt => opt.MapFrom(src => src.Subject.ToString()));

        CreateMap<Document, DocumentDto>()
            .ForMember(dest => dest.CompanyCorporateName, opt => opt.MapFrom(src => src.Company.CorporateName))
            .ForMember(dest => dest.WorkSiteId, opt => opt.MapFrom(src => src.SupplyRequest != null ? src.SupplyRequest.WorkSiteId : (long?)null))
            .ForMember(dest => dest.WorkSiteName, opt => opt.MapFrom(src => src.SupplyRequest != null ? src.SupplyRequest.WorkSite.Name : null))
            .ForMember(dest => dest.DocumentTypeCode, opt => opt.MapFrom(src => src.DocumentType.Code))
            .ForMember(dest => dest.DocumentTypeName, opt => opt.MapFrom(src => src.DocumentType.Name))
            .ForMember(dest => dest.UploadedByName, opt => opt.MapFrom(src => src.UploadedBySupplierUser.Name))
            .ForMember(dest => dest.StatusDescription, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
