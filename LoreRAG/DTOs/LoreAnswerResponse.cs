namespace LoreRAG.DTOs;

public sealed record LoreAnswerResponse(
    string Question,
    string Answer,
    IReadOnlyList<LoreSearchHit> Sources,
    DateTimeOffset GeneratedAtUtc
);