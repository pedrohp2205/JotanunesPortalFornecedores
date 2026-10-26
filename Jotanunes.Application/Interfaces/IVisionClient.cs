using Jotanunes.Application.DTOs.Analysis;

namespace Jotanunes.Application.Interfaces;

public interface IVisionClient
{
    Task<string> ExtractJson(VisionRequest request, CancellationToken cancellationToken = default);
}
