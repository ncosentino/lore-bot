using LoreBot.DTOs;

using Pgvector;

namespace LoreBot;

public interface ILoreRepository
{
    Task<long> InsertAsync(LoreChunk chunk, CancellationToken ct = default);
    Task<IReadOnlyList<LoreSearchHit>> HybridSearchAsync(Vector queryVec, string queryText, int k, CancellationToken ct = default);
    Task<bool> ChunkExistsAsync(string contentHash, CancellationToken ct = default);
}