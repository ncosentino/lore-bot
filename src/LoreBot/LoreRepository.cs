using Dapper;

using LoreBot.DTOs;
using LoreBot.Infrastructure;

using Microsoft.Extensions.Logging;

using Pgvector;

using System.Security.Cryptography;
using System.Text;

namespace LoreBot;

public sealed class LoreRepository(
    IDbConnectionFactory _connectionFactory, 
    ILogger<LoreRepository> _logger) :
    ILoreRepository
{
    public async Task<long> InsertAsync(LoreChunk chunk, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO lore_chunks (
                source_path, anchor_id, title, headings, content,
                tokens, word_count, links_to, updated_at, embedding
            ) VALUES (
                @SourcePath, @AnchorId, @Title, @Headings, @Content,
                @Tokens, @WordCount, @LinksTo, @UpdatedAt, @Embedding
            )
            RETURNING id;";

        using var connection = await _connectionFactory.OpenConnectionAsync(ct);
        
        var id = await connection.QuerySingleAsync<long>(sql, new
        {
            chunk.SourcePath,
            chunk.AnchorId,
            chunk.Title,
            chunk.Headings,
            chunk.Content,
            chunk.Tokens,
            chunk.WordCount,
            chunk.LinksTo,
            chunk.UpdatedAt,
            chunk.Embedding
        });
        
        _logger.LogDebug("Inserted chunk {Id} from {SourcePath}", id, chunk.SourcePath);
        return id;
    }

    public async Task<IReadOnlyList<LoreSearchHit>> HybridSearchAsync(
        Vector queryVec, 
        string queryText, 
        int k, 
        CancellationToken ct = default)
    {
        const string sql = @"
            WITH dense_search AS (
                SELECT 
                    id,
                    source_path,
                    anchor_id,
                    title,
                    headings,
                    content,
                    1 - (embedding <=> @QueryVec) AS dense_score,
                    NULL::float AS sparse_score
                FROM lore_chunks
                ORDER BY embedding <=> @QueryVec
                LIMIT @TopN
            ),
            sparse_search AS (
                SELECT 
                    id,
                    source_path,
                    anchor_id,
                    title,
                    headings,
                    content,
                    NULL::float AS dense_score,
                    ts_rank(tsv, websearch_to_tsquery('english', @QueryText)) AS sparse_score
                FROM lore_chunks
                WHERE tsv @@ websearch_to_tsquery('english', @QueryText)
                ORDER BY ts_rank(tsv, websearch_to_tsquery('english', @QueryText)) DESC
                LIMIT @TopM
            ),
            combined AS (
                SELECT 
                    COALESCE(d.id, s.id) AS id,
                    COALESCE(d.source_path, s.source_path) AS source_path,
                    COALESCE(d.anchor_id, s.anchor_id) AS anchor_id,
                    COALESCE(d.title, s.title) AS title,
                    COALESCE(d.headings, s.headings) AS headings,
                    COALESCE(d.content, s.content) AS content,
                    COALESCE(d.dense_score, 0) AS dense_score,
                    COALESCE(s.sparse_score, 0) AS sparse_score
                FROM dense_search d
                FULL OUTER JOIN sparse_search s ON d.id = s.id
            )
            SELECT 
                id AS Id,
                source_path AS SourcePath,
                anchor_id AS AnchorId,
                title AS Title,
                headings AS Headings,
                ts_headline('english', content, websearch_to_tsquery('english', @QueryText),
                    'MaxFragments=1, MinWords=15, MaxWords=40') AS Excerpt,
                dense_score AS DenseScore,
                sparse_score AS SparseScore,
                (0.65 * dense_score + 0.35 * COALESCE(sparse_score, 0)) AS Score
            FROM combined
            ORDER BY (0.65 * dense_score + 0.35 * COALESCE(sparse_score, 0)) DESC
            LIMIT @K;";

        using var connection = await _connectionFactory.OpenConnectionAsync(ct);
        
        var results = await connection.QueryAsync<LoreSearchHit>(sql, new
        {
            QueryVec = queryVec,
            QueryText = queryText,
            TopN = k * 2,  // Fetch more candidates for dense search
            TopM = k * 2,  // Fetch more candidates for sparse search
            K = k
        });
        
        _logger.LogInformation("Hybrid search for '{Query}' returned {Count} results", queryText, results.Count());
        return results.ToList();
    }

    public async Task<bool> ChunkExistsAsync(string contentHash, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM lore_chunks 
                WHERE content = @ContentHash
            );";

        using var connection = await _connectionFactory.OpenConnectionAsync(ct);
        return await connection.QuerySingleAsync<bool>(sql, new { ContentHash = contentHash });
    }

    public static string ComputeHash(string content)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes);
    }
}