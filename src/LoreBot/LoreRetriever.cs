using LoreBot;
using LoreBot.DTOs;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using System.Text;

namespace LoreRAG;

public class LoreRetriever(
    ILoreRepository _repository,
    IEmbeddingService _embeddingService) : 
    ILoreRetriever
{
    public async Task<LoreSearchResponse> LookupAsync(
        Kernel kernel,
        string query, 
        int k = 6,
        CancellationToken ct = default)
    {
        var queryVector = await _embeddingService.EmbedAsync(
            kernel,
            query,
            ct);
        var hits = await _repository.HybridSearchAsync(
            queryVector,
            query,
            k,
            ct);

        var response = new LoreSearchResponse(
            Question: query,
            Hits: hits,
            GeneratedAtUtc: DateTimeOffset.UtcNow
        );

        return response;
    }

    public async Task<LoreAnswerResponse> AskAsync(
        Kernel kernel, 
        string question, 
        int k = 6,
        CancellationToken ct = default)
    {
        var lookupResponse = await LookupAsync(kernel, question, k, ct);        
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("Based on the following information from the knowledge base:");
        contextBuilder.AppendLine();

        foreach (var hit in lookupResponse.Hits)
        {
            contextBuilder.AppendLine($"**Source: {hit.SourcePath}**");
            if (!string.IsNullOrEmpty(hit.Title))
            {
                contextBuilder.AppendLine($"Section: {hit.Title}");
            }
            contextBuilder.AppendLine($"Excerpt: {hit.Excerpt}");
            contextBuilder.AppendLine($"Relevance Score: {hit.Score:F2}");
            contextBuilder.AppendLine();
        }

        var prompt =
            $"""
                You are a helpful assistant that answers questions based on the provided context from a knowledge base.                

                Context:
                {contextBuilder}

                Question: {question}

                Provide a comprehensive answer based on the context above. If the context doesn't contain enough information to fully answer the question, indicate what information is missing.
                
                ALWAYS cite which sources you're using for your answer.
                NEVER ask follow up questions.
                """;

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var answer = await chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: null,
            kernel: kernel,
            cancellationToken: ct);

        var response = new LoreAnswerResponse(
            Question: question,
            Answer: answer.Content ?? "Unable to generate an answer.",
            Sources: lookupResponse.Hits,
            GeneratedAtUtc: DateTimeOffset.UtcNow
        );
        return response;
    }
}