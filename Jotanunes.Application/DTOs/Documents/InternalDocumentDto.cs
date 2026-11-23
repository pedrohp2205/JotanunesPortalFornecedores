namespace Jotanunes.Application.DTOs.Documents;

public class InternalDocumentDto : DocumentDto
{
    public DateOnly? ExpirationDateForReview { get; set; }
    public DateTime? ExpirationDateReviewedAt { get; set; }
    public DocumentAnalysisSummaryDto? Analysis { get; set; }
}
