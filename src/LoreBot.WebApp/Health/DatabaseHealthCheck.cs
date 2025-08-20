using LoreBot.Infrastructure;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NexusLabs.Needlr;

namespace LoreBot.WebApp.Health;

[DoNotAutoRegister]
public class DatabaseHealthCheck(
    IDbConnectionFactory _connectionPlugin) :
    IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionPlugin.OpenConnectionAsync(cancellationToken);
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