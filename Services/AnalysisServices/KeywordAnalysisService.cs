using glint_backend.Models;
using glint_backend.Interfaces;

namespace glint_backend.Services.AnalysisServices;

public class KeywordAnalysisService : IKeywordAnalysisService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Articles / prepositions / conjunctions
        "the", "and", "for", "with", "that", "this", "from", "into", "onto",
        "over", "under", "such", "than", "then", "them", "they", "their",
        "but", "not", "all", "any", "are", "was", "were", "been", "has",
        "had", "have", "will", "can", "may", "also", "etc", "per", "via",
        "its", "our", "you", "your", "his", "her", "its", "who", "what",
        // Generic job-ad filler
        "new", "use", "using", "used", "some", "more", "well", "good",
        "great", "plus", "role", "work", "about", "other", "join", "help",
        "build", "building", "built", "able", "both", "each", "just",
        "like", "make", "need", "seek", "take", "want", "work", "works",
        "would", "could", "should", "must", "will", "shall", "being",
        "very", "high", "wide", "long", "full", "open", "based", "part",
        "team", "role", "hire", "fast", "best", "real", "true", "self",
        // Generic resume/job filler
        "experience", "experiences", "years", "year", "skills", "skill",
        "required", "requirements", "ability", "abilities", "knowledge",
        "background", "looking", "seeking", "needed", "strong", "solid",
        "proven", "demonstrated", "excellent", "hands", "plus",
        "including", "focused", "driven", "bonus", "preferred",
        "opportunity", "opportunities", "position", "positions",
        "candidate", "candidates", "employer", "employee", "company",
        "office", "remote", "hybrid", "onsite", "location", "apply",
        "applicant", "applicants", "responsible", "responsibilities",
        "qualification", "qualifications", "minimum", "preferred",
    };

    public Task<(decimal Score, string Feedback)> AnalyzeAsync(PdfDocumentData pdfData, JobAdvertisement jobAdvertisement)
    {
        var resumeText = pdfData.Text ?? string.Empty;

        var jobTokens = ExtractTokens(jobAdvertisement.RawText);
        var jobBigrams = ExtractBigrams(jobAdvertisement.RawText);

        var resumeTokens = ExtractTokens(resumeText);
        var resumeBigrams = ExtractBigrams(resumeText);

        // Bigrams are weighted higher (2 pts) — they represent specific phrases
        // e.g. "patient care", "project management", "machine learning"
        var matchedBigrams = jobBigrams.Intersect(resumeBigrams, StringComparer.OrdinalIgnoreCase).ToList();
        var missingBigrams = jobBigrams.Except(resumeBigrams, StringComparer.OrdinalIgnoreCase).ToList();

        var matchedTokens = jobTokens.Intersect(resumeTokens, StringComparer.OrdinalIgnoreCase).ToList();
        var missingTokens = jobTokens.Except(resumeTokens, StringComparer.OrdinalIgnoreCase).ToList();

        // Score: each matched bigram = 2 pts, each matched token = 1 pt
        var totalPoints = (jobBigrams.Count * 2) + jobTokens.Count;
        var matchedPoints = (matchedBigrams.Count * 2) + matchedTokens.Count;

        var score = totalPoints == 0
            ? 100m
            : Math.Clamp(Math.Round((decimal)matchedPoints / totalPoints * 100, 1), 0m, 100m);

        // Build feedback — show most meaningful missing terms first (bigrams first, then tokens)
        var topMissing = missingBigrams
            .Concat(missingTokens)
            .Take(8)
            .ToList();

        var topMatched = matchedBigrams
            .Concat(matchedTokens)
            .Take(8)
            .ToList();

        var lines = new List<string>
        {
            $"Matched {matchedTokens.Count} of {jobTokens.Count} keywords and {matchedBigrams.Count} of {jobBigrams.Count} key phrases."
        };

        if (topMatched.Count > 0)
            lines.Add($"Matched: {string.Join(", ", topMatched)}");

        if (topMissing.Count > 0)
            lines.Add($"Missing: {string.Join(", ", topMissing)}");

        lines.Add(topMissing.Count == 0
            ? "Excellent keyword coverage — your resume closely mirrors the job description."
            : "Tip: incorporate missing terms into your resume where genuinely applicable.");

        return Task.FromResult((score, string.Join("\n", lines)));
    }

    private HashSet<string> ExtractTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        // Don't split on '-' or '.' so terms like C#, ASP.NET,
        // full-stack, well-being, follow-up stay intact
        char[] separators = [' ', '\n', '\r', '\t', ',', ';',
                              '/', '\\', '(', ')', '[', ']',
                              '"', '\'', '–', '—', '|', '!', '?', '@'];

        return text
            .ToLowerInvariant()
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim('.', '_', '*'))
            .Where(t =>
                t.Length > 2 &&
                t.Any(char.IsLetter) &&
                !StopWords.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private List<string> ExtractBigrams(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var tokens = ExtractTokens(text).ToList();

        // Re-extract as ordered list (HashSet loses order) to form sequential bigrams
        char[] separators = [' ', '\n', '\r', '\t', ',', ';',
                              '/', '\\', '(', ')', '[', ']',
                              '"', '\'', '–', '—', '|', '!', '?', '@'];

        var ordered = text
            .ToLowerInvariant()
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim('.', '_', '*'))
            .Where(t =>
                t.Length > 2 &&
                t.Any(char.IsLetter) &&
                !StopWords.Contains(t))
            .ToList();

        var bigrams = new List<string>();
        for (var i = 0; i < ordered.Count - 1; i++)
            bigrams.Add($"{ordered[i]} {ordered[i + 1]}");

        return bigrams.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}