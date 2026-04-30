using System;
using System.Linq;
using glint_backend.Models;
using glint_backend.Interfaces;

namespace glint_backend.Services.AnalysisServices;

/// <summary>
/// AI-based resume analysis using semantic understanding.
/// Placeholder implementation: simulated delay, 50% failure, and richer feedback.
/// </summary>
public class AiAnalysisService : IAiAnalysisService
{
    /// <summary>
    /// Performs AI-based analysis on resume vs job description.
    /// </summary>
    public async Task<(decimal Score, string Feedback)> AnalyzeAsync(string resumeText, string jobText)
    {
        // Simulate realistic network/processing delay for AI service: 2-6 seconds
        var delayMs = Random.Shared.Next(2000, 6001);
        await Task.Delay(delayMs);

        // 50/50 chance to simulate a failure in the placeholder
        if (Random.Shared.Next(0, 2) == 0)
            throw new InvalidOperationException("AI analysis placeholder failed.");

        // Very simple keyword overlap as a signal
        var resumeKeys = ExtractKeywords(resumeText);
        var jobKeys = ExtractKeywords(jobText);
        var matched = resumeKeys.Intersect(jobKeys).ToList();
        var totalJobKeys = Math.Max(1, jobKeys.Count);
        var keywordScore = (decimal)matched.Count / totalJobKeys * 100m;

        // Simulated semantic confidence component (random but biased higher)
        var semanticComponent = (decimal)Random.Shared.Next(60, 96);

        // Weighted final score: 60% semantic, 40% keyword overlap
        var finalScore = Math.Round((semanticComponent * 0.6m) + (keywordScore * 0.4m), 2);

        // Build feedback with some details
        var feedbackLines = new System.Collections.Generic.List<string>();
        feedbackLines.Add($"AI placeholder analysis (simulated). Processing took {delayMs}ms.");
        feedbackLines.Add($"Overall (simulated) fit score: {finalScore} / 100");

        if (matched.Any())
            feedbackLines.Add($"Top matched keywords: {string.Join(", ", matched.Take(10))}");
        else
            feedbackLines.Add("No clear keyword matches found between resume and job description.");

        // Provide actionable suggestions (placeholder)
        feedbackLines.Add("Suggestions: emphasize relevant skills in your summary, add specific keywords from the job description, and quantify achievements where possible.");

        var feedback = string.Join("\n", feedbackLines);
        return (finalScore, feedback);
    }

    private static System.Collections.Generic.List<string> ExtractKeywords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new System.Collections.Generic.List<string>();

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
