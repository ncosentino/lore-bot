using Dapper;
using LoreRAG.Infrastructure.TypeHandlers;
using Pgvector;

namespace LoreRAG.Plugins;

public class DapperVectorTypeHandlerPlugin
{
    private static bool _initialized = false;
    private static readonly object _lock = new();
    private readonly ILogger<DapperVectorTypeHandlerPlugin> _logger;

    public DapperVectorTypeHandlerPlugin(ILogger<DapperVectorTypeHandlerPlugin> logger)
    {
        _logger = logger;
        Initialize();
    }

    public void Initialize()
    {
        lock (_lock)
        {
            if (!_initialized)
            {
                SqlMapper.AddTypeHandler(new VectorTypeHandler());
                _initialized = true;
                _logger.LogInformation("Dapper Vector type handler registered successfully");
            }
        }
    }
}

public static class DapperVectorTypeHandlerPluginExtensions
{
    public static IServiceCollection AddDapperVectorTypeHandlerPlugin(this IServiceCollection services)
    {
        services.AddSingleton<DapperVectorTypeHandlerPlugin>();
        
        // Ensure the plugin is initialized on startup
        services.AddHostedService<DapperVectorTypeHandlerInitializer>();
        
        return services;
    }
}

public class DapperVectorTypeHandlerInitializer : IHostedService
{
    private readonly DapperVectorTypeHandlerPlugin _plugin;

    public DapperVectorTypeHandlerInitializer(DapperVectorTypeHandlerPlugin plugin)
    {
        _plugin = plugin;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _plugin.Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}