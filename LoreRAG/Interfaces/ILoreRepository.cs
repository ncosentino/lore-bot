using LoreRAG.DTOs;
using LoreRAG.Models;
using Pgvector;

namespace LoreRAG.Interfaces;

public interface ILoreRepository
{
    Task<long> InsertAsync(LoreChunk chunk, CancellationToken ct = default);
    Task<IReadOnlyList<LoreSearchHitDto>> HybridSearchAsync(Vector queryVec, string queryText, int k, CancellationToken ct = default);
    Task<bool> ChunkExistsAsync(string contentHash, CancellationToken ct = default);
}