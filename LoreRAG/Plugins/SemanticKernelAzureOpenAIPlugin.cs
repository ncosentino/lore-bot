using LoreRAG.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using NexusLabs.Needlr;

#pragma warning disable SKEXP0010

namespace LoreRAG.Plugins;

internal sealed class SemanticKernelPlugin : IServiceCollectionPlugin
{
    public void Configure(ServiceCollectionPluginOptions options)
    {
        var configuration = options.Config;
        options.Services.Configure<ChatConfiguration>(configuration.GetSection(ChatConfiguration.SectionName));
        options.Services.Configure<EmbeddingConfiguration>(configuration.GetSection(EmbeddingConfiguration.SectionName));

        //options.Services.AddSingleton<SemanticKernelFactory>();
        options.Services.AddSingleton(sp => sp
            .GetRequiredService<SemanticKernelFactory>()
            .GetKernel());
    }
}

public sealed class SemanticKernelFactory
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelFactory> _logger;

    public SemanticKernelFactory(
        IOptions<ChatConfiguration> chatOptions,
        IOptions<EmbeddingConfiguration> embeddingOptions,
        ILogger<SemanticKernelFactory> logger)
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


#pragma warning restore SKEXP0010