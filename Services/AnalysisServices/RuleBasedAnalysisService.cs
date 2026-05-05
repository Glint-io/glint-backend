using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Google.GenAI;
using glint_backend.Interfaces;
using glint_backend.Models;

namespace glint_backend.Services.AnalysisServices;

// Data model representing an individual rule extraction and its validation result
public class RuleCheckResult
{
    [JsonPropertyName("rule")]
    public string Rule { get; set; } = string.Empty;

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("weight")]
    public int Weight { get; set; } = 1;

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;
}

// Wrapper for the collection of rules returned by the AI
public class RuleEvaluationResponse
{
    [JsonPropertyName("checks")]
    public List<RuleCheckResult> Checks { get; set; } = [];
}

public class RuleBasedAnalysisService : IRuleBasedAnalysisService
{
    private readonly Client _client;
    private const string ModelName = "gemini-3.1-flash-lite-preview";
    private const int MaxRetries = 1;

    // Configures JSON parser to handle LLM output quirks like trailing commas
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Initializes the Gemini client using the API key from app settings
    public RuleBasedAnalysisService(IConfiguration configuration)
    {
        var apiKey = configuration["Gemini:ApiKey2"]
            ?? throw new InvalidOperationException("Gemini:ApiKey2 is not configured.");

        _client = new Client(apiKey: apiKey);
    }

    // Main entry point: Orchestrates rule evaluation, scoring, and feedback generation
    public async Task<(decimal Score, string Feedback)> AnalyzeAsync(
        PdfDocumentData pdfData, JobAdvertisement jobAdvertisement)
    {
        var resumeText = pdfData.Text ?? string.Empty;
        var jobText = jobAdvertisement.RawText;

        var checks = await EvaluateRulesAsync(jobText, resumeText);
        var score = CalculateScore(checks);
        var feedback = BuildFeedback(checks);

        return (score, feedback);
    }

    // Sends the prompt to the AI and handles retries or error fallback results
    private async Task<List<RuleCheckResult>> EvaluateRulesAsync(
        string jobText, string resumeText, int retryCount = 0)
    {
        var prompt = BuildPrompt(jobText, resumeText);

        try
        {
            // Request AI to perform the two-step rule extraction and verification
            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: prompt);

            var raw = response.Text ?? string.Empty;
            var json = ExtractJson(raw);
            var result = JsonSerializer.Deserialize<RuleEvaluationResponse>(json, JsonOpts);
            return result?.Checks ?? [];
        }
        // Handle Rate Limiting (429) with a single retry if permitted
        catch (Exception ex) when (Is429(ex) && IsRetryable(ex) && retryCount < MaxRetries)
        {
            await Task.Delay(3000);
            return await EvaluateRulesAsync(jobText, resumeText, retryCount + 1);
        }
        // Handle exhausted quota or specific AI limits with a friendly error "check"
        catch (Exception ex) when (Is429(ex))
        {
            return
            [
                new RuleCheckResult
                {
                    Rule = "AI rule evaluation unavailable",
                    Passed = false,
                    Weight = 0,
                    Detail = ex.Message.Contains("limit: 0")
                        ? "The rule-based analysis has reached its daily limit."
                        : "AI analysis is temporarily at capacity."
                }
            ];
        }
        // Catch-all for unexpected parsing or connection errors
        catch (Exception ex)
        {
            return
            [
                new RuleCheckResult
                {
                    Rule = "Rule evaluation failed",
                    Passed = false,
                    Weight = 0,
                    Detail = ex.Message
                }
            ];
        }
    }

    // Calculates a percentage score based on the weight of passed rules vs total weight
    private static decimal CalculateScore(List<RuleCheckResult> checks)
    {
        var scorable = checks.Where(c => c.Weight > 0).ToList();
        if (scorable.Count == 0) return 0m;

        var total = scorable.Sum(c => c.Weight);
        var earned = scorable.Where(c => c.Passed).Sum(c => c.Weight);

        return Math.Clamp(Math.Round((decimal)earned / total * 100, 2), 0m, 100m);
    }

    // Compiles the individual rule results into a detailed JSON feedback string for the UI
    private static string BuildFeedback(List<RuleCheckResult> checks)
    {
        var passed = checks.Count(c => c.Passed && c.Weight > 0);
        var total = checks.Count(c => c.Weight > 0);
        var criticalFails = checks.Where(c => !c.Passed && c.Weight >= 2).ToList();
        var minorFails = checks.Where(c => !c.Passed && c.Weight == 1).ToList();

        // Categorize results into critical vs minor gaps for better user insight
        return JsonSerializer.Serialize(new
        {
            passed,
            total,
            criticalGapCount = criticalFails.Count,
            minorGapCount = minorFails.Count,
            checks = checks.Select(c => new
            {
                rule = c.Rule,
                passed = c.Passed,
                weight = c.Weight,
                detail = c.Detail,
            }),
            criticalGaps = criticalFails.Select(c => c.Rule),
            minorGaps = minorFails.Select(c => c.Rule),
        }, JsonOpts);
    }

    // Constructs the system prompt that defines the AI's persona and JSON schema requirements
    private static string BuildPrompt(string jobText, string resumeText)
    {
        return $@"You are an objective hiring evaluator. Your task has two steps:

        STEP 1 - Read the job advertisement and extract every non-negotiable and important requirement as a distinct rule.
        STEP 2 - For each rule, evaluate whether the resume satisfies it.

        Rules must be specific and concrete. Examples of good rules:
        - ""Minimum 3 years of backend development experience""
        - ""Proficiency in TypeScript""
        - ""Bachelor's degree in Computer Science or equivalent""
        - ""Experience with CI/CD pipelines""
        - ""Fluency in Swedish""

        Do NOT invent rules that are not in the job ad.
        Do NOT add generic rules like ""good communication skills"" unless the ad explicitly requires them.

        Weight each rule:
        - weight 2 = explicitly marked as required, essential, must-have, or repeated multiple times
        - weight 1 = mentioned once, implied, or listed as preferred/meritorious

        Return ONLY a valid JSON object - no markdown fences, no explanation.

        Schema:
        {{
          ""checks"": [
            {{
              ""rule"": ""Minimum 3 years of backend experience"",
              ""passed"": true,
              ""weight"": 2,
              ""detail"": ""Resume lists 4 years of backend work at Company X.""
            }}
          ]
        }}

        ---

        ## Job Advertisement
        {jobText}

        ## Resume
        {resumeText}";
    }

    // Cleans the raw AI string by stripping Markdown code blocks and extracting the JSON object
    private static string ExtractJson(string raw)
    {
        var clean = Regex.Replace(raw, @"```(?:json)?\s*|\s*```", string.Empty).Trim();
        var start = clean.IndexOf('{');
        var end = clean.LastIndexOf('}');
        if (start >= 0 && end > start)
            return clean[start..(end + 1)];
        return clean;
    }

    // Identifies rate-limiting errors based on response message
    private static bool Is429(Exception ex) =>
        ex.Message.Contains("429") || ex.Message.Contains("TooManyRequests");

    // Determines if a retry is worth attempting (skips if the hard daily limit is hit)
    private static bool IsRetryable(Exception ex) =>
        !ex.Message.Contains("limit: 0");
}