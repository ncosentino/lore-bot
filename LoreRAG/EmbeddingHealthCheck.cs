using LoreRAG.Interfaces;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NexusLabs.Needlr;

[DoNotAutoRegister]
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