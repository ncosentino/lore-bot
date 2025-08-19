using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Options;
using LoreRAG.Configuration;

#pragma warning disable SKEXP0010

namespace LoreRAG.Plugins;

public class SemanticKernelAzureOpenAIPlugin
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelAzureOpenAIPlugin> _logger;

    public SemanticKernelAzureOpenAIPlugin(
        IOptions<ChatConfiguration> chatOptions,
        IOptions<EmbeddingConfiguration> embeddingOptions,
        ILogger<SemanticKernelAzureOpenAIPlugin> logger)
    {
        _logger = logger;
        var chatConfig = chatOptions.Value;
        var embeddingConfig = embeddingOptions.Value;
        
        // Validate configuration
        chatConfig.Validate();
        embeddingConfig.Validate();

        var builder = Kernel.CreateBuilder();
        
        // Add Chat Completion based on provider
        switch (chatConfig.Provider?.ToLower())
        {
            case "azure-openai":
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: chatConfig.AzureOpenAI!.Deployment,
                    endpoint: chatConfig.AzureOpenAI.Endpoint,
                    apiKey: chatConfig.AzureOpenAI.Key
                );
                _logger.LogInformation("Configured Azure OpenAI chat with deployment: {Deployment}", 
                    chatConfig.AzureOpenAI.Deployment);
                break;
            case "openai":
                builder.AddOpenAIChatCompletion(
                    modelId: chatConfig.OpenAI!.Model,
                    apiKey: chatConfig.OpenAI.ApiKey
                );
                _logger.LogInformation("Configured OpenAI chat with model: {Model}", 
                    chatConfig.OpenAI.Model);
                break;
        }
        
        // Add Text Embeddings based on provider
        switch (embeddingConfig.Provider?.ToLower())
        {
            case "azure-openai":
                builder.AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: embeddingConfig.AzureOpenAI!.Deployment,
                    endpoint: embeddingConfig.AzureOpenAI.Endpoint,
                    apiKey: embeddingConfig.AzureOpenAI.Key,
                    dimensions: embeddingConfig.Dimensions
                );
                _logger.LogInformation("Configured Azure OpenAI embeddings with deployment: {Deployment}, dimensions: {Dimensions}", 
                    embeddingConfig.AzureOpenAI.Deployment, embeddingConfig.Dimensions);
                break;
            case "openai":
                builder.AddOpenAITextEmbeddingGeneration(
                    modelId: embeddingConfig.OpenAI!.Model,
                    apiKey: embeddingConfig.OpenAI.ApiKey,
                    dimensions: embeddingConfig.Dimensions
                );
                _logger.LogInformation("Configured OpenAI embeddings with model: {Model}, dimensions: {Dimensions}", 
                    embeddingConfig.OpenAI.Model, embeddingConfig.Dimensions);
                break;
        }

        _kernel = builder.Build();
    }

    public Kernel GetKernel() => _kernel;
}

public static class SemanticKernelAzureOpenAIPluginExtensions
{
    public static IServiceCollection AddSemanticKernelAzureOpenAIPlugin(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options from configuration
        services.Configure<ChatConfiguration>(configuration.GetSection(ChatConfiguration.SectionName));
        services.Configure<EmbeddingConfiguration>(configuration.GetSection(EmbeddingConfiguration.SectionName));
        
        services.AddSingleton<SemanticKernelAzureOpenAIPlugin>();
        services.AddSingleton(sp => sp.GetRequiredService<SemanticKernelAzureOpenAIPlugin>().GetKernel());
        return services;
    }
}

#pragma warning restore SKEXP0010