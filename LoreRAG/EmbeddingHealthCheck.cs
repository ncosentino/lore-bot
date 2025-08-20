using LoreRAG;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using NexusLabs.Needlr;

[DoNotAutoRegister]
public sealed class EmbeddingHealthCheck(
    IEmbeddingService embeddingService,
    SemanticKernelFactory _semanticKernelFactory) :
    IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testText = "health check";
            var kernel = _semanticKernelFactory.Build();
            var embedding = await embeddingService.EmbedAsync(kernel, testText, cancellationToken);
            
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