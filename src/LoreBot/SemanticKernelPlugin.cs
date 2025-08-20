using LoreBot.Configuration;

using Microsoft.Extensions.DependencyInjection;

using NexusLabs.Needlr;

namespace LoreBot;

internal sealed class SemanticKernelPlugin : IServiceCollectionPlugin
{
    public void Configure(ServiceCollectionPluginOptions options)
    {
        var configuration = options.Config;
        options.Services.Configure<ChatConfiguration>(configuration.GetSection(ChatConfiguration.SectionName));
        options.Services.Configure<EmbeddingConfiguration>(configuration.GetSection(EmbeddingConfiguration.SectionName));
    }
}
