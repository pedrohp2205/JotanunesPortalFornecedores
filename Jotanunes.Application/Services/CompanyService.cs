using AutoMapper;
using Jotanunes.Application.DTOs;
using Jotanunes.Application.DTOs.Companies;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Pagination;
using Jotanunes.Domain.Validation;

namespace Jotanunes.Application.Services;

public class CompanyService : ICompanyService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(IMapper mapper, IUnitOfWork unitOfWork)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<PageList<CompanyDto>> Get(PageParams pageParams, CompanyFilter filter)
    {
        PageList<Company> companies = await _unitOfWork.CompanyRepository.Get(pageParams, filter);
        var companyDtos = _mapper.Map<IEnumerable<CompanyDto>>(companies.Items);
        return new PageList<CompanyDto>(companyDtos.ToList(), companies.TotalCount, pageParams.PageNumber, pageParams.PageSize);
    }

    public async Task<CompanyDto> GetById(long id)
    {
        Company? company = await _unitOfWork.CompanyRepository.GetById(id);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }
        return _mapper.Map<CompanyDto>(company);
    }

    public async Task<CompanyDto> Create(CompanyCreateDto model)
    {
        var cnpj = Cnpj.Normalize(model.Cnpj);
        JotanunesException.When(
            await _unitOfWork.CompanyRepository.CnpjInUse(cnpj),
            "Já existe uma empresa cadastrada com este CNPJ.");

        var company = new Company(
            model.Cnpj,
            model.CorporateName,
            model.TradeName,
            model.Email,
            model.Phone,
            model.ResponsibleName,
            BuildAddress(model.Address),
            model.StateRegistration);

        _unitOfWork.CompanyRepository.Add(company);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CompanyDto>(company);
    }

    public async Task<CompanyDto> Update(long id, CompanyUpdateDto model)
    {
        Company? company = await _unitOfWork.CompanyRepository.GetById(id);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        company.Update(
            model.CorporateName,
            model.TradeName,
            model.Email,
            model.Phone,
            model.ResponsibleName,
            BuildAddress(model.Address),
            model.StateRegistration);

        _unitOfWork.CompanyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CompanyDto>(company);
    }

    public async Task<CompanyDto> Delete(long id)
    {
        Company? company = await _unitOfWork.CompanyRepository.GetById(id);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        _unitOfWork.CompanyRepository.Delete(company);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CompanyDto>(company);
    }

    private static Address BuildAddress(AddressDto model)
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
