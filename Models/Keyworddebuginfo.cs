namespace glint_backend.Models;

/// <summary>
/// Full extraction internals returned by the keyword debug endpoint.
/// Lets you see exactly what tokens/bigrams were pulled from a job ad
/// before any CV comparison happens.
/// </summary>
public record KeywordDebugInfo
{
    public required string ScoredText { get; init; }
    public required string ScoredTextLengthVsTotal { get; init; }
    public required string SectionStartedAt { get; init; }
    public required IReadOnlyList<DebugToken> ImportantTokens { get; init; }
    public required IReadOnlyList<string> GeneralTokens { get; init; }
    public required IReadOnlyList<string> Bigrams { get; init; }
    public required IReadOnlyDictionary<string, int> TokenFrequency { get; init; }
}

/// <summary>An important token with its frequency and the reason it was promoted.</summary>
public record DebugToken(string Token, int Frequency, string Reason);