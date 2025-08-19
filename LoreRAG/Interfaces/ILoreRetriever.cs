using LoreRAG.DTOs;

namespace LoreRAG.Interfaces;

public interface ILoreRetriever
{
    Task<LoreSearchResponse> AskAsync(string question, int k = 6, CancellationToken ct = default);
}