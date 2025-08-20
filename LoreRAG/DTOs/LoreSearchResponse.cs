namespace LoreRAG.DTOs;

public sealed record LoreSearchResponse(
    string Question,
    IReadOnlyList<LoreSearchHit> Hits,
    DateTimeOffset GeneratedAtUtc
);