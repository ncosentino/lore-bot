using LoreRAG.Configuration;

using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

using Pgvector;

#pragma warning disable SKEXP0001

namespace LoreRAG;

public sealed class EmbeddingService(
    ILogger<EmbeddingService> _logger) : 
    IEmbeddingService
{
    public async Task<Vector> EmbedAsync(
        Kernel kernel,
        string text, 
        CancellationToken ct = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Attempted to embed null or empty text, skipping embedding generation");
            // Skip embedding for empty text - caller should handle this case
            throw new ArgumentException("Cannot generate embedding for null or empty text", nameof(text));
        }
        
        // Check token limit (rough estimate: 1 token ≈ 4 characters)
        // text-embedding-3-small has a max context of 8192 tokens
        const int maxTokens = 8000; // Leave some buffer
        var estimatedTokens = text.Length / 4;
        if (estimatedTokens > maxTokens)
        {
            _logger.LogError(
                "Text exceeds maximum token limit. Estimated tokens: {EstimatedTokens}, Max: {MaxTokens}, Text length: {Length}", 
                estimatedTokens, 
                maxTokens, 
                text.Length);
            throw new ArgumentException($"Text is too long for embedding. Estimated {estimatedTokens} tokens exceeds limit of {maxTokens}", nameof(text));
        }
        
        try
        {
            var embeddingService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            var embeddings = await embeddingService.GenerateEmbeddingAsync(text, kernel, ct);
            var floatArray = embeddings.ToArray();
            
            _logger.LogDebug("Generated embedding with {Dimensions} dimensions for text of length {Length}", 
                floatArray.Length, text.Length);
            
            return new Vector(floatArray);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding for text of length {Length}", text.Length);
            throw;
        }
    }
}

#pragma warning restore SKEXP0001