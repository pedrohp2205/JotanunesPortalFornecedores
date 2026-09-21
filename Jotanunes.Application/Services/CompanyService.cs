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

        var company = CompanyFactory.Build(model);

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
            CompanyFactory.BuildAddress(model.Address),
            model.StateRegistration);

        _unitOfWork.CompanyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CompanyDto>(company);
    }

    public async Task<CompanyDto> ChangeSupplierType(long id, CompanyChangeSupplierTypeDto model)
    {
        Company? company = await _unitOfWork.CompanyRepository.GetById(id);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        // Tipos que a empresa deixa de fornecer não podem ter solicitação ativa.
        var removedTypes = company.SupplierType & ~model.SupplierType;
        JotanunesException.When(
            removedTypes != 0 && await _unitOfWork.SupplyRequestRepository.HasActive(id, removedTypes),
            "Existem solicitações ativas do tipo de fornecimento que está sendo removido. Conclua ou cancele antes de alterar.");

        company.ChangeSupplierType(model.SupplierType);

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
}
