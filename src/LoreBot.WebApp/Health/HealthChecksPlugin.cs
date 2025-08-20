using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using NexusLabs.Needlr.AspNet;

using System.Text.Json;

namespace LoreBot.WebApp.Health;

internal sealed class HealthChecksPlugin :
    IWebApplicationBuilderPlugin,
    IWebApplicationPlugin
{
    public void Configure(WebApplicationBuilderPluginOptions options)
    {
        options.Builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "postgres" })
            .AddCheck<EmbeddingHealthCheck>("embeddings", tags: new[] { "ai", "embeddings" })
            ;
    }

    public void Configure(WebApplicationPluginOptions options)
    {
        var app = options.WebApplication;
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        description = x.Value.Description,
                        duration = x.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("ai")
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Name == "self"
        });
    }
}