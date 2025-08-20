using LoreRAG.DTOs;

using Microsoft.SemanticKernel;

namespace LoreRAG;

public interface ILoreRetriever
{
    Task<LoreSearchResponse> LookupAsync(
        Kernel kernel, 
        string query, 
        int k = 6, 
        CancellationToken ct = default);
    
    Task<LoreAnswerResponse> AskAsync(
        Kernel kernel, 
        string question, 
        int k = 6,
        CancellationToken ct = default);
}