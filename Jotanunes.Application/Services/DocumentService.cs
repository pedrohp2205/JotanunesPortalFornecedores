using AutoMapper;
using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Interfaces;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Application.Services;

public class DocumentService : IDocumentService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentStorageService _storageService;
    private readonly IDocumentComplianceService _complianceService;

    public DocumentService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IDocumentStorageService storageService,
        IDocumentComplianceService complianceService)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _complianceService = complianceService;
    }

    public async Task<PageList<DocumentDto>> Get(PageParams pageParams, DocumentFilter filter)
    {
        var documents = await _unitOfWork.DocumentRepository.Get(pageParams, filter);
        var dtos = _mapper.Map<IEnumerable<DocumentDto>>(documents.Items);
        return new PageList<DocumentDto>(dtos.ToList(), documents.TotalCount, pageParams.PageNumber, pageParams.PageSize);
    }

    public async Task<DocumentDto> GetById(long id, long? companyId = null)
    {
        var document = await GetExisting(id, companyId);
        return _mapper.Map<DocumentDto>(document);
    }

    public async Task<DocumentDto> Upload(
        long companyId,
        long uploadedBySupplierUserId,
        DocumentUploadDto model,
        Stream fileContent,
        string originalFileName,
        string contentType)
    {
        var documentType = await _unitOfWork.DocumentTypeRepository.GetById(model.DocumentTypeId);
        if (documentType is null || !documentType.Active)
        {
            throw new KeyNotFoundException("Tipo de documento não encontrado");
        }

        var referencePeriodStart = model.ReferencePeriodStart;
        var referencePeriodEnd = model.ReferencePeriodEnd;

        if (model.CompanyWorkSiteId.HasValue)
        {
            var companyWorkSite = await _unitOfWork.WorkSiteRepository.GetLinkById(model.CompanyWorkSiteId.Value);
            if (companyWorkSite is null || companyWorkSite.CompanyId != companyId)
            {
                throw new KeyNotFoundException("Solicitação (empresa e obra) não encontrada");
            }

            if (documentType.Category == DocumentCategory.Recurring && (referencePeriodStart is null || referencePeriodEnd is null))
            {
                var currentPeriod = companyWorkSite.WorkSite.GetCurrentPeriod(DateOnly.FromDateTime(DateTime.UtcNow));
                referencePeriodStart ??= currentPeriod.Start;
                referencePeriodEnd ??= currentPeriod.End;
            }
        }

        var storageKey = $"companies/{companyId}/documents/{Guid.NewGuid()}-{SanitizeFileName(originalFileName)}";

        var document = new Document(
            companyId,
            model.DocumentTypeId,
            uploadedBySupplierUserId,
            documentType.Category,
            documentType.Subject,
            storageKey,
            originalFileName,
            contentType,
            model.CompanyWorkSiteId,
            model.WorkerName,
            model.WorkerCpf,
            referencePeriodStart,
            referencePeriodEnd,
            model.ExpirationDate);

        await _storageService.UploadAsync(storageKey, fileContent, contentType);

        _unitOfWork.DocumentRepository.Add(document);
        await _unitOfWork.SaveChangesAsync();

        return await GetById(document.Id);
    }

    public async Task<DocumentDownloadDto> Download(long id, long? companyId = null)
    {
        var document = await GetExisting(id, companyId);
        var content = await _storageService.DownloadAsync(document.StorageKey);

        return new DocumentDownloadDto
        {
            Content = content,
            ContentType = document.ContentType,
            FileName = document.OriginalFileName
        };
    }

    public async Task<DocumentDto> Approve(long id)
    {
        var document = await GetExisting(id);

        document.Approve();

        _unitOfWork.DocumentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync();

        if (document.DocumentType.Category == DocumentCategory.Onboarding)
        {
            await TryMarkCompanyEligible(document.CompanyId);
        }

        return _mapper.Map<DocumentDto>(document);
    }

    public async Task<DocumentDto> Reject(long id, string reason)
    {
        var document = await GetExisting(id);

        document.Reject(reason);

        _unitOfWork.DocumentRepository.Update(document);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentDto>(document);
    }

    private async Task TryMarkCompanyEligible(long companyId)
    {
        if (!await _complianceService.IsOnboardingComplete(companyId))
        {
            return;
        }

        var company = await _unitOfWork.CompanyRepository.GetById(companyId);
        if (company is null)
        {
            return;
        }

        company.MarkEligible();

        _unitOfWork.CompanyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync();
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
    }

    private async Task<Document> GetExisting(long id, long? companyId = null)
    {
        var document = await _unitOfWork.DocumentRepository.GetById(id);
        if (document is null || (companyId.HasValue && document.CompanyId != companyId.Value))
        {
            throw new KeyNotFoundException("Documento não encontrado");
        }

        return document;
    }
}
