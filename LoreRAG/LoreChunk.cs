using Pgvector;

namespace LoreRAG;

public class LoreChunk
{
    public long Id { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string? AnchorId { get; set; }
    public string? Title { get; set; }
    public string[]? Headings { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Tokens { get; set; }
    public int? WordCount { get; set; }
    public string[]? LinksTo { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Vector Embedding { get; set; } = new Vector(new float[768]);
    public string? ContentHash { get; set; }
}