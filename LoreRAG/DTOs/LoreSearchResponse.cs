namespace LoreRAG.DTOs;

public sealed record LoreSearchResponse(
    string Question,
    IReadOnlyList<LoreSearchHitDto> Hits,
    DateTimeOffset GeneratedAtUtc
);