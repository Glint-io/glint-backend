using System;
using System.Linq;
using System.Collections.Generic;
using glint_backend.Models;
using glint_backend.Interfaces;

namespace glint_backend.Services.AnalysisServices;

/// <summary>
/// Rule-based resume analysis using industry standards.
/// Placeholder implementation: simulated delay, 50% failure, and structured feedback.
/// </summary>
public class RuleBasedAnalysisService : IRuleBasedAnalysisService
{
    /// <summary>
    /// Performs rule-based analysis on resume vs job description.
    /// </summary>
    public async Task<(decimal Score, string Feedback)> AnalyzeAsync(string resumeText, string jobText)
    {
        // Simulate realistic CPU-bound processing delay: 300-1200ms
        var delayMs = Random.Shared.Next(300, 1201);
        await Task.Delay(delayMs);

        // 50/50 chance to simulate a failure in the placeholder
        if (Random.Shared.Next(0, 2) == 0)
            throw new InvalidOperationException("Rule-based analysis placeholder failed.");

        var resumeKeys = ExtractKeywords(resumeText);
        var jobKeys = ExtractKeywords(jobText);

        // Simple mocked heuristics
        var skillsMatched = resumeKeys.Intersect(jobKeys).Count();
        var skillCoverage = Math.Round((decimal)skillsMatched / Math.Max(1, jobKeys.Count) * 100, 2);

        // Mock experience and education scoring as random small heuristics
        var experienceScore = Random.Shared.Next(40, 91); // 40-90
        var educationScore = Random.Shared.Next(30, 91); // 30-90

        // Weighted score: skills 50%, experience 30%, education 20%
        var finalScore = Math.Round((skillCoverage * 0.5m) + (experienceScore * 0.3m) + (educationScore * 0.2m), 2);

        // Build feedback
        var feedbackLines = new List<string>
        {
            $"Rule-based placeholder analysis. Processing took {delayMs}ms.",
            $"Final score (simulated): {finalScore} / 100",
            $"Skill coverage: {skillCoverage}% ({skillsMatched} matched keywords)",
            $"Experience heuristic: {experienceScore} / 100",
            $"Education heuristic: {educationScore} / 100",
            "Suggestions: ensure required skills are prominent, add measurable achievements, and align job titles where relevant."
        };

        var feedback = string.Join("\n", feedbackLines);
        return (finalScore, feedback);
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
