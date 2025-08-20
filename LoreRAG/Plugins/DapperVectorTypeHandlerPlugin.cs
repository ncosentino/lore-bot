using Dapper;

using LoreRAG.Infrastructure.TypeHandlers;

using NexusLabs.Needlr;

namespace LoreRAG.Plugins;

internal sealed class TypeHandlerPlugin : IServiceCollectionPlugin
{
    public void Configure(ServiceCollectionPluginOptions options)
    {
        SqlMapper.AddTypeHandler(new VectorTypeHandler());
    }
}
