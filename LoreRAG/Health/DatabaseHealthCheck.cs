using LoreRAG;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NexusLabs.Needlr;

namespace LoreRAG.Health;

[DoNotAutoRegister]
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly PostgresConnectionFactory _connectionPlugin;

    public DatabaseHealthCheck(PostgresConnectionFactory connectionPlugin)
    {
        _connectionPlugin = connectionPlugin;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionPlugin.CreateConnectionAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.ExecuteScalar();

            return HealthCheckResult.Healthy("Database connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}