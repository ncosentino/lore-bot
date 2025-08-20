using Microsoft.SemanticKernel;

using Pgvector;

namespace LoreBot;

public interface IEmbeddingService
{
    Task<Vector> EmbedAsync(
        Kernel kernel,
        string text, 
        CancellationToken ct = default);
}