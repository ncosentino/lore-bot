namespace LoreBot.DTOs;

public sealed record LoreSearchHit(
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