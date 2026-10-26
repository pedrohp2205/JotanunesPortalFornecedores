namespace Jotanunes.Domain.Enums;

public enum DocumentAnalysisStatus
{
    Pending = 1,
    Completed = 2,
    ManualReviewRequired = 3,
    Failed = 4,
    NotSupported = 5
}
