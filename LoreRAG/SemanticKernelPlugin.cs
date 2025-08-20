using LoreRAG.Configuration;

using NexusLabs.Needlr;

namespace LoreRAG;

internal sealed class SemanticKernelPlugin : IServiceCollectionPlugin
{
    public void Configure(ServiceCollectionPluginOptions options)
    {
        var configuration = options.Config;
        options.Services.Configure<ChatConfiguration>(configuration.GetSection(ChatConfiguration.SectionName));
        options.Services.Configure<EmbeddingConfiguration>(configuration.GetSection(EmbeddingConfiguration.SectionName));
    }
}
