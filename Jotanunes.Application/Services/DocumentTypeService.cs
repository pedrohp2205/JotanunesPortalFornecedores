using AutoMapper;
using Jotanunes.Application.DTOs.DocumentTypes;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;
using Jotanunes.Domain.Interfaces;

namespace Jotanunes.Application.Services;

public class DocumentTypeService : IDocumentTypeService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DocumentTypeService(IMapper mapper, IUnitOfWork unitOfWork)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<DocumentTypeDto>> Get()
    {
        var documentTypes = await _unitOfWork.DocumentTypeRepository.Get();
        return _mapper.Map<List<DocumentTypeDto>>(documentTypes);
    }

    public async Task<List<SupplierDocumentTypeDto>> GetForCompany(long companyId, SupplierType? supplierType = null)
    {
        var company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            throw new KeyNotFoundException("Empresa não encontrada");
        }

        var types = supplierType.HasValue ? supplierType.Value & company.SupplierType : company.SupplierType;
        if (types == 0)
        {
            return [];
        }

        var documentTypes = await _unitOfWork.DocumentTypeRepository.GetApplicable(types);
        return _mapper.Map<List<SupplierDocumentTypeDto>>(documentTypes);
    }

    public async Task<DocumentTypeDto> GetById(long id)
    {
        var documentType = await GetExisting(id);
        return _mapper.Map<DocumentTypeDto>(documentType);
    }

    public async Task<DocumentTypeDto> Create(DocumentTypeCreateDto model)
    {
        JotanunesException.When(
            await _unitOfWork.DocumentTypeRepository.CodeInUse(model.Code),
            "Já existe um tipo de documento com este código.");

        var documentType = new DocumentType(
            model.Code,
            model.Name,
            model.Category,
            model.AppliesTo,
            model.Subject,
            model.RequiresExpirationDate,
            model.IsConditional,
            model.ConditionDescription);

        _unitOfWork.DocumentTypeRepository.Add(documentType);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentTypeDto>(documentType);
    }

    public async Task<DocumentTypeDto> Update(long id, DocumentTypeUpdateDto model)
    {
        var documentType = await GetExisting(id);

        documentType.Update(
            model.Name,
            model.Category,
            model.AppliesTo,
            model.Subject,
            model.RequiresExpirationDate,
            model.IsConditional,
            model.ConditionDescription);

        _unitOfWork.DocumentTypeRepository.Update(documentType);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentTypeDto>(documentType);
    }

    public async Task<DocumentTypeDto> Delete(long id)
    {
        var documentType = await GetExisting(id);

        documentType.Deactivate();

        _unitOfWork.DocumentTypeRepository.Update(documentType);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentTypeDto>(documentType);
    }

    private async Task<DocumentType> GetExisting(long id)
    {
        var documentType = await _unitOfWork.DocumentTypeRepository.GetById(id);
        if (documentType is null)
        {
            throw new KeyNotFoundException("Tipo de documento não encontrado");
        }

        return documentType;
    }
}
