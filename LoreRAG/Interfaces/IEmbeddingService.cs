using Pgvector;

namespace LoreRAG.Interfaces;

public interface IEmbeddingService
{
    Task<Vector> EmbedAsync(string text, CancellationToken ct = default);
}