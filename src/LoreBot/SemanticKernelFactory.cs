using LoreBot.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

using NexusLabs.Needlr;

namespace LoreBot;

public sealed class SemanticKernelFactory(
    IOptions<ChatConfiguration> _chatOptions,
    IOptions<EmbeddingConfiguration> _embeddingOptions,
    IServiceProvider _serviceProvider,
    ILogger<SemanticKernelFactory> _logger)
{
    private readonly Lazy<IKernelBuilder> _lazyKernelBuilder = new(() =>
    {
        // Validate configuration
        var chatConfig = _chatOptions.Value;
        chatConfig.Validate();

        var embeddingConfig = _embeddingOptions.Value;
        embeddingConfig.Validate();

        var builder = Kernel.CreateBuilder();
        builder.AddLoreFunctions(_serviceProvider);

        // FIXME: update this to use the latest Needlr API so that
        // we have better access to these service descriptors
        foreach (var d in _serviceProvider.GetServiceRegistrations(x =>
        {
            builder.Services.Add(x);
            return true;
        }))
        {
        }

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
#pragma warning disable SKEXP0010
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
#pragma warning restore SKEXP0010
        }

        return builder;
    });

    public Kernel Build()
    {
        var builder = _lazyKernelBuilder.Value;
        var kernel = builder.Build();
        return kernel;
    }
}