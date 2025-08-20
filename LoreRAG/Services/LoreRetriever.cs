using LoreRAG.DTOs;
using LoreRAG.Interfaces;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using System.Diagnostics;
using System.Text;

namespace LoreRAG.Services;

public class LoreRetriever : ILoreRetriever
{
    private readonly ILoreRepository _repository;
    private readonly IEmbeddingService _embeddingService;
    private readonly Kernel _kernel;
    private readonly ILogger<LoreRetriever> _logger;

    public LoreRetriever(
        ILoreRepository repository,
        IEmbeddingService embeddingService,
        Kernel kernel,
        ILogger<LoreRetriever> logger)
    {
        _repository = repository;
        _embeddingService = embeddingService;
        _kernel = kernel;
        _logger = logger;
    }

    public async Task<LoreSearchResponse> LookupAsync(string query, int k = 6, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Generate embedding for the query
            var embeddingStopwatch = Stopwatch.StartNew();
            var queryVector = await _embeddingService.EmbedAsync(query, ct);
            embeddingStopwatch.Stop();
            _logger.LogInformation("Generated embedding for query in {ElapsedMs}ms", embeddingStopwatch.ElapsedMilliseconds);
            
            // Perform hybrid search
            var searchStopwatch = Stopwatch.StartNew();
            var hits = await _repository.HybridSearchAsync(queryVector, query, k, ct);
            searchStopwatch.Stop();
            _logger.LogInformation("Hybrid search completed in {ElapsedMs}ms with {Count} results", 
                searchStopwatch.ElapsedMilliseconds, hits.Count);
            
            // Create response
            var response = new LoreSearchResponse(
                Question: query,
                Hits: hits,
                GeneratedAtUtc: DateTimeOffset.UtcNow
            );
            
            stopwatch.Stop();
            _logger.LogInformation("Total lookup time: {ElapsedMs}ms for query: {Query}", 
                stopwatch.ElapsedMilliseconds, query);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve results for query: {Query}", query);
            throw;
        }
    }

    public async Task<LoreAnswerResponse> AskAsync(string question, int k = 6, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // First, perform the lookup to get relevant context
            var lookupResponse = await LookupAsync(question, k, ct);
            
            // Prepare context from search results
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
            
            // Prepare the prompt for the chat completion
            var prompt = 
                $"""
                You are a helpful assistant that answers questions based on the provided context from a knowledge base.                

                Context:
                {contextBuilder}

                Question: {question}

                Please provide a comprehensive answer based on the context above. If the context doesn't contain enough information to fully answer the question, indicate what information is missing. Always cite which sources you're using for your answer.
                """;

            // Get chat completion service
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            
            // Generate the answer
            var chatStopwatch = Stopwatch.StartNew();
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);
            
            var answer = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: null,
                kernel: _kernel,
                cancellationToken: ct);
            
            chatStopwatch.Stop();
            _logger.LogInformation("Generated answer in {ElapsedMs}ms", chatStopwatch.ElapsedMilliseconds);
            
            // Create response
            var response = new LoreAnswerResponse(
                Question: question,
                Answer: answer.Content ?? "Unable to generate an answer.",
                Sources: lookupResponse.Hits,
                GeneratedAtUtc: DateTimeOffset.UtcNow
            );
            
            stopwatch.Stop();
            _logger.LogInformation("Total ask time: {ElapsedMs}ms for question: {Question}", 
                stopwatch.ElapsedMilliseconds, question);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate answer for question: {Question}", question);
            throw;
        }
    }
}