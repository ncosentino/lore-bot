using LoreRAG.Ingestion;

using NexusLabs.Needlr.AspNet;

using Serilog;

internal sealed class IngestPlugin : IWebApplicationPlugin
{
    public void Configure(WebApplicationPluginOptions options)
    {
        options.WebApplication.MapPost("/api/ingest", async (string path, IngestionService ingestionService) =>
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
    }
}
