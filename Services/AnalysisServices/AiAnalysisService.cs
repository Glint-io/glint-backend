using System.Text.RegularExpressions;
using Google.GenAI;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using glint_backend.Models;

namespace glint_backend.Services.AnalysisServices;

public class AiAnalysisService : IAiAnalysisService
{
    private readonly Client _client;
    private const string ModelName = "gemini-3.1-flash-lite-preview";
    private const int MaxRetries = 1;

    private static readonly Regex ScoreRegex = new(
        @"match\s*score\s*[:\-]?\s*\*{0,2}\s*([0-9]{1,3}(?:\.[0-9]{1,2})?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FallbackScoreRegex = new(
        @"score[^\d]*([0-9]{1,3}(?:\.[0-9]{1,2})?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public AiAnalysisService(IConfiguration configuration)
    {
        var apiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");

        _client = new Client(apiKey: apiKey);
    }

    public async Task<(decimal Score, string Feedback)> AnalyzeAsync(
        PdfDocumentData pdfData, JobAdvertisement jobAdvertisement)
    {
        var prompt = BuildPrompt(pdfData.Text ?? string.Empty, jobAdvertisement.RawText);
        return await ExecuteWithRetryAsync(prompt, retryCount: 0);
    }

    private async Task<(decimal Score, string Feedback)> ExecuteWithRetryAsync(
        string prompt, int retryCount)
    {
        try
        {
            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: prompt
            );

            var rawText = response.Text ?? string.Empty;
            
            // Check if the response contains error indicators instead of a valid analysis
            if (DetectErrorInResponse(rawText, out var errorMessage))
            {
                throw new AiServiceUnavailableException(errorMessage);
            }

            var score = ExtractScore(rawText);
            var feedback = StripScoreLine(rawText).Trim();

            return (score, feedback);
        }
        // Retry for temporary 429s
        catch (Exception ex) when (Is429(ex) && IsRetryable(ex) && retryCount < MaxRetries)
        {
            await Task.Delay(3000);
            return await ExecuteWithRetryAsync(prompt, retryCount + 1);
        }
        // Final handling for 429 (no silent fallback) — bubble up as typed exception
        catch (Exception ex) when (Is429(ex))
        {
            throw new AiServiceUnavailableException(BuildQuotaMessage(ex), ex);
        }
        // Any other unexpected AI client errors should also surface to the caller
        catch (Exception ex)
        {
            throw new AiServiceUnavailableException($"AI analysis currently unavailable: {ex.Message}", ex);
        }
    }

    private static bool Is429(Exception ex) =>
        ex.Message.Contains("429") || ex.Message.Contains("TooManyRequests");

    private static bool IsRetryable(Exception ex) =>
        !ex.Message.Contains("limit: 0");

    /// <summary>
    /// Detects if the response text contains error indicators rather than a valid analysis.
    /// Common error patterns:
    ///   - API key issues ("API key", "leaked", "invalid", "unauthorized")
    ///   - Quota/rate limit errors ("quota", "limit", "exhausted", "too many")
    ///   - Service errors ("error", "failed", "unavailable", "exception")
    ///   - Malformed responses that don't contain a score
    /// </summary>
    private static bool DetectErrorInResponse(string text, out string errorMessage)
    {
        var lower = text.ToLowerInvariant();

        // API key/authentication errors
        if (lower.Contains("api key") || lower.Contains("leaked") || 
            lower.Contains("unauthorized") || lower.Contains("invalid key"))
        {
            errorMessage = "AI service authentication failed. Please check your API configuration.";
            return true;
        }

        // Quota exhausted / rate limiting
        if (lower.Contains("quota") || lower.Contains("exhausted") || 
            lower.Contains("daily limit") || lower.Contains("limit: 0"))
        {
            errorMessage = "AI analysis quota exceeded. Please try again later.";
            return true;
        }

        // Generic error phrases in response
        if (lower.Contains("error") || lower.Contains("failed") || 
            lower.Contains("exception") || lower.Contains("unavailable"))
        {
            // Only treat as error if it looks like an error message, not feedback
            // (i.e., if "error" appears at the start or the response lacks a score line)
            if (text.StartsWith("Error", StringComparison.OrdinalIgnoreCase) || 
                text.StartsWith("Failed", StringComparison.OrdinalIgnoreCase) ||
                !ScoreRegex.IsMatch(text))
            {
                errorMessage = "AI analysis encountered an error. Please try again.";
                return true;
            }
        }

        errorMessage = string.Empty;
        return false;
    }

    private static string BuildQuotaMessage(Exception ex)
    {
        if (ex.Message.Contains("limit: 0"))
            return "AI analysis is unavailable: the Gemini free-tier quota for this project is 0. Please enable billing or wait for the daily quota to reset.";

        return "AI analysis is temporarily unavailable due to rate limiting. Please try again shortly.";
    }

    // BuildPrompt, ExtractScore, StripScoreLine unchanged...
    private static string BuildPrompt(string resumeText, string jobText) => $"""
    You are an expert Technical Recruiter and Career Coach with 20 years of experience in talent acquisition.
    Be direct and specific, but frame all feedback constructively — assume the candidate is motivated and capable of making improvements.

    Conduct a deep-dive gap analysis between the resume and job advertisement provided below.

    ## Analysis Requirements
    1. **Keyword Match**: Identify essential hard skills, software, and certifications mentioned in the job ad that are missing or underemphasized in the resume.
    2. **Experience Alignment**: Evaluate if the seniority level and specific responsibilities in the resume align with the job's core KPIs.
    3. **Soft Skill Evidence**: Check if the resume demonstrates (rather than just lists) the soft skills required (e.g., leadership, communication).
    4. **Missing "Must-Haves"**: Explicitly list any deal-breakers the resume is currently missing.

    ## Output Format
    Respond using EXACTLY this structure — do not deviate:

    Match Score: [number between 0 and 100, up to two decimal places]

    **Missing Elements**
    - [gap or missing element 1]
    - [gap or missing element 2]
    - ...

    **Actionable Fixes**
    1. [specific rewrite suggestion]
    2. [specific rewrite suggestion]
    3. [specific rewrite suggestion]
    4. [specific rewrite suggestion]
    5. [specific rewrite suggestion]
    (add up to 5 if needed)

    IMPORTANT:
    - The "Match Score:" line must appear exactly once, on its own line, at the very top.
    - Do NOT embed the score anywhere else in the response.
    - The only section headings allowed are "Missing Elements" and "Actionable Fixes".
    - Do NOT add preamble or closing remarks outside the defined sections.

    ---

    ## Job Advertisement
    {jobText}

    ## Resume
    {resumeText}
    """;

    private static decimal ExtractScore(string text)
    {
        var match = ScoreRegex.Match(text);

        if (!match.Success)
            match = FallbackScoreRegex.Match(text);

        if (match.Success && decimal.TryParse(
                match.Groups[1].Value,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed))
        {
            return Math.Clamp(Math.Round(parsed, 2), 0m, 100m);
        }

        return 0m;
    }

    private static string StripScoreLine(string text) =>
        Regex.Replace(
            text,
            @"(?im)^.*match\s*score\s*[:\-]?.*$\r?\n?",
            string.Empty).Trim();
}