using Microsoft.SemanticKernel;

using Pgvector;

namespace LoreRAG;

public interface IEmbeddingService
{
    Task<Vector> EmbedAsync(
        Kernel kernel,
        string text, 
        CancellationToken ct = default);
}