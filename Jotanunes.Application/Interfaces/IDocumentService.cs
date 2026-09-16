using Jotanunes.Application.DTOs.Documents;
using Jotanunes.Domain.Filters;
using Jotanunes.Domain.Pagination;

namespace Jotanunes.Application.Interfaces;

public interface IDocumentService
{
    Task<PageList<DocumentDto>> Get(PageParams pageParams, DocumentFilter filter);
    Task<DocumentDto> GetById(long id, long? companyId = null);
    Task<DocumentDto> Upload(
        long companyId,
        long uploadedBySupplierUserId,
        DocumentUploadDto model,
        Stream fileContent,
        string originalFileName,
        string contentType);
    Task<DocumentDownloadDto> Download(long id, long? companyId = null);
    Task<DocumentDto> Approve(long id);
    Task<DocumentDto> Reject(long id, string reason);
}
