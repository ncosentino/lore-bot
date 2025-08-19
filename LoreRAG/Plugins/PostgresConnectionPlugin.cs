using Npgsql;
using Pgvector.Npgsql;
using System.Data;

namespace LoreRAG.Plugins;

public class PostgresConnectionPlugin
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgresConnectionPlugin> _logger;

    public PostgresConnectionPlugin(IConfiguration configuration, ILogger<PostgresConnectionPlugin> logger)
    {
        _logger = logger;
        
        var connectionString = configuration["DB_CONN"] 
            ?? Environment.GetEnvironmentVariable("DB_CONN")
            ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=lore;Pooling=true";
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        
        _dataSource = dataSourceBuilder.Build();
        _logger.LogInformation("PostgreSQL connection plugin initialized with pgvector support");
    }

    public IDbConnection CreateConnection()
    {
        return _dataSource.CreateConnection();
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken ct = default)
    {
        var connection = await _dataSource.OpenConnectionAsync(ct);
        return connection;
    }
}

public static class PostgresConnectionPluginExtensions
{
    public static IServiceCollection AddPostgresConnectionPlugin(this IServiceCollection services)
    {
        services.AddSingleton<PostgresConnectionPlugin>();
        services.AddTransient<IDbConnection>(sp => sp.GetRequiredService<PostgresConnectionPlugin>().CreateConnection());
        return services;
    }
}