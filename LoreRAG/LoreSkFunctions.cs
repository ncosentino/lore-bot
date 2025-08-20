using Microsoft.SemanticKernel;

using System.ComponentModel;
using System.Text.Json;

namespace LoreRAG;

public class LoreSkFunctions
{
    private readonly ILoreRetriever _retriever;
    private readonly ILogger<LoreSkFunctions> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public LoreSkFunctions(ILoreRetriever retriever, ILogger<LoreSkFunctions> logger)
    {
        _retriever = retriever;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [KernelFunction]
    [Description("Search the lore knowledge base for information about a topic or question")]
    public async Task<string> AskAsync(
        Kernel kernel,
        [Description("The question or topic to search for in the lore knowledge base")] string question,
        [Description("The maximum number of results to return (default: 6)")] int k = 6)
    {
        try
        {
            _logger.LogInformation("SK function called with question: {Question}, k: {K}", question, k);

            var response = await _retriever.AskAsync(kernel, question, k);

            // Return as JSON string for the LLM to process
            var json = JsonSerializer.Serialize(response, _jsonOptions);

            _logger.LogDebug("SK function returning {Length} characters of JSON", json.Length);

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute SK function for question: {Question}", question);

            // Return error as structured JSON
            var errorResponse = new
            {
                error = true,
                message = "Failed to search lore knowledge base",
                details = ex.Message
            };

            return JsonSerializer.Serialize(errorResponse, _jsonOptions);
        }
    }
}

// FIXME: this is a great opportunity to figure out how to extend
// needlr to support semantic kernel functions
public static class LoreSkFunctionsExtensions
{
    public static IKernelBuilder AddLoreFunctions(this IKernelBuilder builder, IServiceProvider services)
    {
        var retriever = services.GetRequiredService<ILoreRetriever>();
        var logger = services.GetRequiredService<ILogger<LoreSkFunctions>>();

        var functions = new LoreSkFunctions(retriever, logger);
        builder.Plugins.AddFromObject(functions, "LoreKnowledge");

        return builder;
    }
}