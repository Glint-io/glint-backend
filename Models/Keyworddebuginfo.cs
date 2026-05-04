namespace glint_backend.Models;

/// <summary>
/// Full extraction internals returned by the keyword debug endpoint.
/// Lets you see exactly what tokens/bigrams were pulled from a job ad
/// before any CV comparison happens.
/// </summary>
public record KeywordDebugInfo
{
    /// <summary>Raw text that was actually scored (after section extraction).</summary>
    public required string ScoredText { get; init; }

    /// <summary>
    /// Important tokens: rare in general English OR repeated ≥ 2× in the ad.
    /// These are weighted 2× in scoring.
    /// </summary>
    public required IReadOnlyList<DebugToken> ImportantTokens { get; init; }

    /// <summary>
    /// General tokens: common English words with no special weight.
    /// </summary>
    public required IReadOnlyList<string> GeneralTokens { get; init; }

    /// <summary>All bigrams extracted from the requirements section.</summary>
    public required IReadOnlyList<string> Bigrams { get; init; }

    /// <summary>Frequency map of every token in the scored text.</summary>
    public required IReadOnlyDictionary<string, int> TokenFrequency { get; init; }
}

/// <summary>An important token with its frequency and the reason it was promoted.</summary>
public record DebugToken(string Token, int Frequency, string Reason);