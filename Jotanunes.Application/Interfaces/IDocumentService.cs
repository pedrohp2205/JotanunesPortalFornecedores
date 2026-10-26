using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentService
{
    Task<PageList<DocumentDto>> Get(PageParams pageParams, DocumentFilter filter);
    Task<DocumentDto> GetById(long id, long? companyId = null);
    Task<PageList<InternalDocumentDto>> GetForReview(PageParams pageParams, InternalDocumentFilter filter);
    Task<InternalDocumentDto> GetByIdForReview(long id);
    Task<DocumentDto> Upload(
        long companyId,
        long uploadedBySupplierUserId,
        DocumentUploadDto model,
        Stream fileContent,
        string originalFileName);
    Task<DocumentDownloadDto> Download(long id, long? companyId = null);
    Task<InternalDocumentDto> Approve(long id);
    Task<InternalDocumentDto> Reject(long id, string reason);
}
