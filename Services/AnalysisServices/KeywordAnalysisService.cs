using System;
using System.Linq;
using System.Collections.Generic;
using glint_backend.Models;
using glint_backend.Interfaces;

namespace glint_backend.Services.AnalysisServices;

/// <summary>
/// Keyword-based resume analysis.
/// Placeholder implementation: simulated delay, 50% failure, and improved feedback.
/// </summary>
public class KeywordAnalysisService : IKeywordAnalysisService
{
    /// <summary>
    /// Performs keyword-based analysis on resume vs job description.
    /// </summary>
    public async Task<(decimal Score, string Feedback)> AnalyzeAsync(PdfDocumentData pdfData, string jobText)
    {
        // Simulate quick processing delay: 150-500ms
        var delayMs = Random.Shared.Next(150, 501);
        await Task.Delay(delayMs);

        // 50/50 chance to simulate failure
        if (Random.Shared.Next(0, 2) == 0)
            throw new InvalidOperationException("Keyword analysis placeholder failed.");

        var resumeText = pdfData.Text ?? string.Empty;

        var resumeKeys = ExtractKeywords(resumeText);
        var jobKeys = ExtractKeywords(jobText);
        var matched = resumeKeys.Intersect(jobKeys).ToList();
        var missing = jobKeys.Except(resumeKeys).Take(10).ToList();
        var total = Math.Max(1, jobKeys.Count);
        var score = Math.Round((decimal)matched.Count / total * 100, 2);

        var feedbackLines = new List<string>
        {
            $"Keyword analysis placeholder (simulated). Processing took {delayMs}ms.",
            $"Matched {matched.Count} of {total} job keywords. Score: {score} / 100"
        };

        if (matched.Any())
            feedbackLines.Add($"Top matches: {string.Join(", ", matched.Take(10))}");

        if (missing.Any())
            feedbackLines.Add($"Important missing keywords to consider adding: {string.Join(", ", missing)}");

        feedbackLines.Add("Suggestion: add missing keywords to relevant sections, but ensure verifiable context.");

        var feedback = string.Join("\n", feedbackLines);
        return (score, feedback);
    }

    private List<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var separators = new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '/', '\\', '(', ')', '[', ']', '"', '\'', '-', '_'};
        var tokens = text
            .ToLowerInvariant()
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => t.Length > 2)
            .Distinct()
            .ToList();

        return tokens;
    }
}
