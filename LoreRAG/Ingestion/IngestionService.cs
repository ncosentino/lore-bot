using LoreRAG.Interfaces;
using LoreRAG.Repositories;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace LoreRAG.Ingestion;

public class IngestionService
{
    private readonly ILoreRepository _repository;
    private readonly IEmbeddingService _embeddingService;
    private readonly MarkdownChunker _chunker;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        ILoreRepository repository,
        IEmbeddingService embeddingService,
        MarkdownChunker chunker,
        ILogger<IngestionService> logger)
    {
        _repository = repository;
        _embeddingService = embeddingService;
        _chunker = chunker;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestDirectoryAsync(string directoryPath, CancellationToken ct = default)
    {
        var result = new IngestionResult();
        var stopwatch = Stopwatch.StartNew();
        
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }
        
        // Find all markdown files
        var markdownFiles = Directory.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories);
        _logger.LogInformation("Found {Count} markdown files in {Path}", markdownFiles.Length, directoryPath);
        
        foreach (var filePath in markdownFiles)
        {
            try
            {
                var fileResult = await IngestFileAsync(filePath, ct);
                result.FilesProcessed++;
                result.ChunksCreated += fileResult.ChunksCreated;
                result.ChunksSkipped += fileResult.ChunksSkipped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest file: {FilePath}", filePath);
                result.Errors.Add($"{filePath}: {ex.Message}");
            }
        }
        
        stopwatch.Stop();
        result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        
        _logger.LogInformation(
            "Ingestion completed: {Files} files, {Created} chunks created, {Skipped} chunks skipped in {Ms}ms",
            result.FilesProcessed, result.ChunksCreated, result.ChunksSkipped, result.ElapsedMilliseconds);
        
        return result;
    }

    public async Task<FileIngestionResult> IngestFileAsync(string filePath, CancellationToken ct = default)
    {
        var result = new FileIngestionResult { FilePath = filePath };
        _logger.LogInformation("Ingesting file: {FilePath}", filePath);
        
        // Read file content
        var content = await File.ReadAllTextAsync(filePath, ct);
        
        // Create chunks
        var chunks = _chunker.ChunkMarkdownFile(filePath, content);
        
        foreach (var chunk in chunks)
        {
            // Skip empty chunks
            if (string.IsNullOrWhiteSpace(chunk.Content))
            {
                _logger.LogWarning("Skipping empty chunk from {FilePath}", filePath);
                result.ChunksSkipped++;
                continue;
            }
            
            // Compute content hash for idempotency
            var contentHash = ComputeHash(chunk.Content);
            chunk.ContentHash = contentHash;
            
            // Check if chunk already exists
            if (await _repository.ChunkExistsAsync(contentHash, ct))
            {
                _logger.LogDebug("Chunk already exists, skipping: {Hash}", contentHash);
                result.ChunksSkipped++;
                continue;
            }
            
            // Generate embedding
            try
            {
                chunk.Embedding = await _embeddingService.EmbedAsync(chunk.Content, ct);
                
                // Insert chunk
                var id = await _repository.InsertAsync(chunk, ct);
                _logger.LogDebug("Inserted chunk {Id} from {FilePath}", id, filePath);
                result.ChunksCreated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to embed/insert chunk from {FilePath}", filePath);
                throw;
            }
        }
        
        _logger.LogInformation(
            "File ingestion completed: {FilePath} - {Created} created, {Skipped} skipped",
            filePath, result.ChunksCreated, result.ChunksSkipped);
        
        return result;
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes);
    }
}

public class IngestionResult
{
    public int FilesProcessed { get; set; }
    public int ChunksCreated { get; set; }
    public int ChunksSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public long ElapsedMilliseconds { get; set; }
}

public class FileIngestionResult
{
    public string FilePath { get; set; } = string.Empty;
    public int ChunksCreated { get; set; }
    public int ChunksSkipped { get; set; }
}