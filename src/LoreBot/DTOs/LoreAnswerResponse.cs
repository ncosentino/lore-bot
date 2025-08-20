namespace LoreBot.DTOs;

public sealed record LoreAnswerResponse(
    string Question,
    string Answer,
    IReadOnlyList<LoreSearchHit> Sources,
    DateTimeOffset GeneratedAtUtc
);