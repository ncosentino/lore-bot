using LoreRAG.DTOs;

namespace LoreRAG.Interfaces;

public interface ILoreRetriever
{
    Task<LoreSearchResponse> LookupAsync(string query, int k = 6, CancellationToken ct = default);
    Task<LoreAnswerResponse> AskAsync(string question, int k = 6, CancellationToken ct = default);
}