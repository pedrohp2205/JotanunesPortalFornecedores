using System.Text.Json;
using AutoMapper;
using Jotanunes.Application.DTOs.Analysis;
using Jotanunes.Application.Exceptions;
using Jotanunes.Application.Interfaces;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jotanunes.Application.Services;

public class DocumentAnalysisService : IDocumentAnalysisService
{
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentStorageService _storageService;
    private readonly IReadOnlyList<IDocumentTextExtractor> _extractors;
    private readonly IReadOnlyList<IDocumentTypeAnalyzer> _analyzers;
    private readonly IDocumentRasterizer _rasterizer;
    private readonly IVisionClient? _visionClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DocumentAnalysisService> _logger;

    public DocumentAnalysisService(
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IDocumentStorageService storageService,
        IEnumerable<IDocumentTextExtractor> extractors,
        IEnumerable<IDocumentTypeAnalyzer> analyzers,
        IDocumentRasterizer rasterizer,
        TimeProvider timeProvider,
        ILogger<DocumentAnalysisService> logger,
        IVisionClient? visionClient = null)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _extractors = extractors.OrderBy(e => e.Engine).ToList();
        _analyzers = analyzers.ToList();
        _rasterizer = rasterizer;
        _visionClient = visionClient;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<List<long>> GetPendingIds(int take)
    {
        return await _unitOfWork.DocumentAnalysisRepository.GetPendingIds(take, _timeProvider.GetUtcNow().UtcDateTime);
    }

    public async Task Analyze(long analysisId, CancellationToken cancellationToken = default)
    {
        var analysis = await _unitOfWork.DocumentAnalysisRepository.GetById(analysisId);
        if (analysis is null || analysis.Status != DocumentAnalysisStatus.Pending)
        {
            return;
        }

        var document = analysis.Document;
        var analyzer = _analyzers.FirstOrDefault(a =>
            a.DocumentTypeCodes.Contains(document.DocumentType.Code, StringComparer.OrdinalIgnoreCase));

        if (analyzer is null)
        {
            analysis.MarkNotSupported();
        }
        else
        {
            try
            {
                await RunFallbackChain(analysis, analyzer, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                var transient = ex is TransientAnalysisException;
                _logger.LogWarning(ex, "Falha ao analisar o documento {DocumentId} (tentativa {Attempt}, temporária: {Transient}).", document.Id, analysis.Attempts + 1, transient);
                analysis.RegisterFailure(ex.Message, transient, _timeProvider.GetUtcNow().UtcDateTime);
            }
        }

        _unitOfWork.DocumentAnalysisRepository.Update(analysis);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<DocumentAnalysisDto> GetByDocument(long documentId)
    {
        var analysis = await _unitOfWork.DocumentAnalysisRepository.GetByDocumentId(documentId)
            ?? throw new KeyNotFoundException("Análise do documento não encontrada");

        return _mapper.Map<DocumentAnalysisDto>(analysis);
    }

    public async Task<DocumentAnalysisDto> Reanalyze(long documentId)
    {
        var analysis = await _unitOfWork.DocumentAnalysisRepository.GetByDocumentId(documentId);

        if (analysis is null)
        {
            var document = await _unitOfWork.DocumentRepository.GetById(documentId)
                ?? throw new KeyNotFoundException("Documento não encontrado");

            analysis = new DocumentAnalysis(document);
            _unitOfWork.DocumentAnalysisRepository.Add(analysis);
        }
        else
        {
            analysis.Restart();
            _unitOfWork.DocumentAnalysisRepository.Update(analysis);
        }

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<DocumentAnalysisDto>(analysis);
    }

    private async Task RunFallbackChain(DocumentAnalysis analysis, IDocumentTypeAnalyzer analyzer, CancellationToken cancellationToken)
    {
        var document = analysis.Document;

        using var content = new MemoryStream();
        await using (var stored = await _storageService.DownloadAsync(document.StorageKey, cancellationToken))
        {
            await stored.CopyToAsync(content, cancellationToken);
        }

        FieldExtraction? best = null;
        TextExtractionEngine? bestEngine = null;

        foreach (var extractor in _extractors)
        {
            content.Position = 0;
            var text = await extractor.Extract(content, document.ContentType, cancellationToken);
            var extraction = analyzer.Extract(text);

            if (TryFinish(analysis, analyzer, extractor.Engine, extraction))
            {
                return;
            }

            if (best is null || extraction.Fields.Count > best.Fields.Count)
            {
                best = extraction;
                bestEngine = extractor.Engine;
            }
        }

        if (analyzer is IVisionAnalyzer visionAnalyzer && _visionClient is not null)
        {
            content.Position = 0;
            var extraction = await ReadWithVision(visionAnalyzer, content, document.ContentType, cancellationToken);

            if (extraction is not null)
            {
                if (TryFinish(analysis, analyzer, TextExtractionEngine.Vision, extraction))
                {
                    return;
                }

                if (best is null || extraction.Fields.Count > best.Fields.Count)
                {
                    best = extraction;
                    bestEngine = TextExtractionEngine.Vision;
                }
            }
        }

        var reason = best is null
            ? "Nenhum método de leitura disponível para este arquivo."
            : $"Não foi possível ler: {string.Join(", ", best.Missing)}.";

        analysis.RequireManualReview(bestEngine, best?.Fields ?? [], reason);
    }

    private bool TryFinish(DocumentAnalysis analysis, IDocumentTypeAnalyzer analyzer, TextExtractionEngine engine, FieldExtraction extraction)
    {
        var document = analysis.Document;

        if (extraction.WrongDocument is not null)
        {
            analysis.Complete(engine, extraction.Fields, [
                new AnalysisFinding(
                    "WRONG_DOCUMENT_TYPE",
                    FindingSeverity.Blocking,
                    $"O arquivo parece ser {extraction.WrongDocument}, e não {document.DocumentType.Name}.")
            ]);
            return true;
        }

        if (!extraction.IsComplete)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        analysis.Complete(engine, extraction.Fields, analyzer.Validate(extraction, document, today));
        return true;
    }

    private async Task<FieldExtraction?> ReadWithVision(
        IVisionAnalyzer analyzer,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var images = await _rasterizer.Render(content, contentType, cancellationToken);
        if (images.Count == 0)
        {
            return null;
        }

        var json = await _visionClient!.ExtractJson(
            new VisionRequest(analyzer.Instructions, analyzer.SchemaName, analyzer.JsonSchema, images),
            cancellationToken);

        using var result = JsonDocument.Parse(json);
        return analyzer.FromVision(result.RootElement);
    }
}
