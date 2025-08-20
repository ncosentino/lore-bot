using Dapper;

using NexusLabs.Needlr;

namespace LoreBot.Infrastructure;

internal sealed class TypeHandlerPlugin : IServiceCollectionPlugin
{
    public void Configure(ServiceCollectionPluginOptions options)
    {
        SqlMapper.AddTypeHandler(new VectorTypeHandler());
    }
}
