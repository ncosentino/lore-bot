using LoreRAG.DTOs;
using LoreRAG.Interfaces;
using System.Diagnostics;

namespace LoreRAG.Services;

public class LoreRetriever : ILoreRetriever
{
    private readonly ILoreRepository _repository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<LoreRetriever> _logger;

    public LoreRetriever(
        ILoreRepository repository,
        IEmbeddingService embeddingService,
        ILogger<LoreRetriever> logger)
    {
        _repository = repository;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<LoreSearchResponse> AskAsync(string question, int k = 6, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Generate embedding for the question
            var embeddingStopwatch = Stopwatch.StartNew();
            var queryVector = await _embeddingService.EmbedAsync(question, ct);
            embeddingStopwatch.Stop();
            _logger.LogInformation("Generated embedding for question in {ElapsedMs}ms", embeddingStopwatch.ElapsedMilliseconds);
            
            // Perform hybrid search
            var searchStopwatch = Stopwatch.StartNew();
            var hits = await _repository.HybridSearchAsync(queryVector, question, k, ct);
            searchStopwatch.Stop();
            _logger.LogInformation("Hybrid search completed in {ElapsedMs}ms with {Count} results", 
                searchStopwatch.ElapsedMilliseconds, hits.Count);
            
            // Create response
            var response = new LoreSearchResponse(
                Question: question,
                Hits: hits,
                GeneratedAtUtc: DateTimeOffset.UtcNow
            );
            
            stopwatch.Stop();
            _logger.LogInformation("Total retrieval time: {ElapsedMs}ms for question: {Question}", 
                stopwatch.ElapsedMilliseconds, question);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve results for question: {Question}", question);
            throw;
        }
    }
}