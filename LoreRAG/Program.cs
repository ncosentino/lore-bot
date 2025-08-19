using LoreRAG.Ingestion;
using LoreRAG.Interfaces;
using LoreRAG.Plugins;
using LoreRAG.Repositories;
using LoreRAG.Services;
using LoreRAG.SK;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Initialize Serilog
builder.AddSerilogPlugin();

try
{
    Log.Information("Starting LoreRAG application...");
    
    // Add plugins
    builder.Services.AddPostgresConnectionPlugin();
    builder.Services.AddDapperVectorTypeHandlerPlugin();
    builder.Services.AddSemanticKernelAzureOpenAIPlugin(builder.Configuration);
    
    // Add repositories
    builder.Services.AddScoped<ILoreRepository, LoreRepository>();
    
    // Add services
    builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
    builder.Services.AddScoped<ILoreRetriever, LoreRetriever>();
    builder.Services.AddScoped<MarkdownChunker>();
    builder.Services.AddScoped<IngestionService>();
    builder.Services.AddScoped<LoreSkFunctions>();
    
    // Add controllers
    builder.Services.AddControllers();
    
    // Add API documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() 
        { 
            Title = "LoreRAG API", 
            Version = "v1",
            Description = "Retrieval-Augmented Generation system for game lore knowledge base"
        });
    });
    
    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy())
        .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "postgres" })
        .AddCheck<EmbeddingHealthCheck>("embeddings", tags: new[] { "ai", "embeddings" });
    
    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
    
    var app = builder.Build();
    
    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "LoreRAG API v1");
            c.RoutePrefix = string.Empty;
        });
    }
    
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.MapControllers();
    
    // Map health check endpoints
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
    
    // Map ingestion endpoint for testing (should be a background job in production)
    app.MapPost("/api/ingest", async (string path, IngestionService ingestionService) =>
    {
        try
        {
            var result = await ingestionService.IngestDirectoryAsync(path);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ingestion failed");
            return Results.Problem(ex.Message);
        }
    });
    
    Log.Information("LoreRAG application started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Health check implementations
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly PostgresConnectionPlugin _connectionPlugin;
    
    public DatabaseHealthCheck(PostgresConnectionPlugin connectionPlugin)
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

public class EmbeddingHealthCheck : IHealthCheck
{
    private readonly IEmbeddingService _embeddingService;
    
    public EmbeddingHealthCheck(IEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testText = "health check";
            var embedding = await _embeddingService.EmbedAsync(testText, cancellationToken);
            
            if (embedding != null && embedding.ToArray().Length > 0)
            {
                return HealthCheckResult.Healthy("Embedding service is healthy");
            }
            
            return HealthCheckResult.Degraded("Embedding service returned empty result");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Embedding service failed", ex);
        }
    }
}