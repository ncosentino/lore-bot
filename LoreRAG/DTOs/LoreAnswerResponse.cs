namespace LoreRAG.DTOs;

public sealed record LoreAnswerResponse(
    string Question,
    string Answer,
    IReadOnlyList<LoreSearchHitDto> Sources,
    DateTimeOffset GeneratedAtUtc
);