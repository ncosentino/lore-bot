namespace LoreRAG.DTOs;

public sealed record LoreSearchHitDto(
    long Id,
    string SourcePath,
    string? AnchorId,
    string? Title,
    Array? Headings,
    string Excerpt,
    double DenseScore,
    float? SparseScore,
    double Score
);