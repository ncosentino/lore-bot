using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

using System.Data;

namespace LoreBot.Infrastructure.Postgres;

internal sealed class PostgresConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgresConnectionFactory> _logger;

    public PostgresConnectionFactory(
        IConfiguration configuration,
        ILogger<PostgresConnectionFactory> logger)
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

    public async Task<IDbConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = await _dataSource.OpenConnectionAsync(ct);
        return connection;
    }
}
