using System.Runtime.CompilerServices;
using System.Threading.Channels;
using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using glint_backend.Models;

namespace glint_backend.Services;

public class AnalysisService(
    IAnalysisRepository analysisRepo,
    IResumeRepository resumeRepo,
    IJobAdvertisementRepository jobAdRepo,
    IFileValidationService fileValidator) : IAnalysisService
{
    // ── Standard (await all 3) ────────────────────────────────────────────────

    public async Task<AnalyzeResponse> AnalyzeGuestAsync(byte[] pdfBytes, string jobText)
    {
        var results = await RunAllMethodsAsync(Guid.Empty, pdfBytes, jobText);

        return new AnalyzeResponse
        {
            AnalysisId = Guid.Empty,
            Label = null,
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Results = MapResults(results)
        };
    }

    public async Task<AnalyzeResponse> AnalyzeAuthenticatedAsync(Guid userId, AnalyzeRequest request)
    {
        var (pdfBytes, resumeId) = await ResolveResumeAsync(userId, request);
        var (analysis, _) = await CreateAnalysisAsync(userId, resumeId, request);

        var results = await RunAllMethodsAsync(analysis.Id, pdfBytes, request.JobText);

        foreach (var r in results)
            await analysisRepo.AddResultAsync(r);

        analysis.Status = AnalysisStatus.Completed;
        await analysisRepo.UpdateAnalysisAsync(analysis);

        return new AnalyzeResponse
        {
            AnalysisId = analysis.Id,
            Label = analysis.Label,
            Status = analysis.Status,
            CreatedAt = analysis.CreatedAt,
            Results = MapResults(results)
        };
    }

    // ── SSE streaming ─────────────────────────────────────────────────────────

    public async IAsyncEnumerable<AnalysisStreamEvent> StreamGuestAsync(
        byte[] pdfBytes, string jobText,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<AnalysisStreamEvent>();
        bool isFirst = true;

        var tasks = Enum.GetValues<AnalysisMethod>().Select(async method =>
        {
            var result = await RunMethodSafeAsync(Guid.Empty, method, pdfBytes, jobText);
            var evt = new AnalysisStreamEvent
            {
                AnalysisId = isFirst ? Guid.Empty : null,
                Result = MapResult(result),
                EventType = "result"
            };
            isFirst = false;
            await channel.Writer.WriteAsync(evt, ct);
        });

        _ = Task.WhenAll(tasks).ContinueWith(_ => channel.Writer.Complete(), CancellationToken.None);

        await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            yield return evt;
    }

    public async IAsyncEnumerable<AnalysisStreamEvent> StreamAuthenticatedAsync(
        Guid userId, AnalyzeRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (pdfBytes, resumeId) = await ResolveResumeAsync(userId, request);
        var (analysis, _) = await CreateAnalysisAsync(userId, resumeId, request);

        var channel = Channel.CreateUnbounded<AnalysisStreamEvent>();
        bool isFirst = true;

        var tasks = Enum.GetValues<AnalysisMethod>().Select(async method =>
        {
            var result = await RunMethodSafeAsync(analysis.Id, method, pdfBytes, request.JobText);
            await analysisRepo.AddResultAsync(result);

            var evt = new AnalysisStreamEvent
            {
                AnalysisId = isFirst ? analysis.Id : null,
                Label = isFirst ? analysis.Label : null,
                Result = MapResult(result),
                EventType = "result"
            };
            isFirst = false;

            await channel.Writer.WriteAsync(evt, ct);
        });

        _ = Task.WhenAll(tasks).ContinueWith(async _ =>
        {
            analysis.Status = AnalysisStatus.Completed;
            await analysisRepo.UpdateAnalysisAsync(analysis);
            channel.Writer.Complete();
        }, CancellationToken.None);

        await foreach (var evt in channel.Reader.ReadAllAsync(ct))
            yield return evt;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    // Resolves PDF bytes from either a saved resume or an uploaded file.
    // Uploaded files are NOT persisted — only saved resumes are.
    private async Task<(byte[] pdfBytes, Guid? resumeId)> ResolveResumeAsync(
        Guid userId, AnalyzeRequest request)
    {
        if (request.ResumeId.HasValue)
        {
            var saved = await resumeRepo.GetByIdAsync(request.ResumeId.Value)
                ?? throw new NotFoundException("Saved resume not found.");

            if (saved.UserId != userId)
                throw new NotFoundException("Saved resume not found.");

            return (saved.FileData, saved.Id);
        }

        var validation = await fileValidator.ValidatePdfAsync(request.Resume!);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);

        // File is used for analysis only — not saved to the database
        return (validation.FileBytes!, null);
    }

    private async Task<(Analysis analysis, JobAdvertisement jobAd)> CreateAnalysisAsync(
        Guid userId, Guid? resumeId, AnalyzeRequest request)
    {
        var jobAd = new JobAdvertisement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RawText = request.JobText,
            CreatedAt = DateTime.UtcNow
        };
        await jobAdRepo.AddAsync(jobAd);

        var analysis = new Analysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResumeId = resumeId,
            JobAdvertisementId = jobAd.Id,
            Label = request.Label,
            CreatedAt = DateTime.UtcNow,
            Status = AnalysisStatus.InProgress
        };
        await analysisRepo.AddAnalysisAsync(analysis);

        return (analysis, jobAd);
    }

    private static async Task<List<AnalysisResult>> RunAllMethodsAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        var tasks = Enum.GetValues<AnalysisMethod>()
            .Select(m => RunMethodSafeAsync(analysisId, m, pdfBytes, jobText));

        return [.. await Task.WhenAll(tasks)];
    }

    // Catches any method failure and returns a 0-score result instead of throwing.
    private static async Task<AnalysisResult> RunMethodSafeAsync(
        Guid analysisId, AnalysisMethod method, byte[] pdfBytes, string jobText)
    {
        try
        {
            return await RunMethodAsync(analysisId, method, pdfBytes, jobText);
        }
        catch (Exception ex)
        {
            return new AnalysisResult
            {
                Id = Guid.NewGuid(),
                AnalysisId = analysisId,
                Method = method,
                Score = 0,
                Feedback = $"{method} analysis failed: {ex.Message}",
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    private static Task<AnalysisResult> RunMethodAsync(
        Guid analysisId, AnalysisMethod method, byte[] pdfBytes, string jobText)
        => method switch
        {
            AnalysisMethod.AI => RunAiAsync(analysisId, pdfBytes, jobText),
            AnalysisMethod.RuleBased => RunRuleBasedAsync(analysisId, pdfBytes, jobText),
            AnalysisMethod.Keyword => RunKeywordAsync(analysisId, pdfBytes, jobText),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };

    private static AnalysisResultResponse MapResult(AnalysisResult r) => new()
    {
        Id = r.Id,
        Method = r.Method.ToString(),
        Score = r.Score,
        Feedback = r.Feedback,
        CompletedAt = r.CompletedAt
    };

    private static List<AnalysisResultResponse> MapResults(List<AnalysisResult> results) =>
        results.Select(MapResult).ToList();

    // ── Method stubs (replace with real implementations) ─────────────────────

    private static async Task<AnalysisResult> RunAiAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        await Task.Delay(Random.Shared.Next(1000, 4000));

        if (Random.Shared.NextDouble() < 0.5)
            throw new Exception("Simulated AI failure.");

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            Method = AnalysisMethod.AI,
            Score = Math.Round((decimal)(Random.Shared.NextDouble() * 100), 2),
            Feedback = "AI score not yet implemented.",
            CompletedAt = DateTime.UtcNow
        };
    }

    private static async Task<AnalysisResult> RunRuleBasedAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        await Task.Delay(Random.Shared.Next(1000, 4000));

        if (Random.Shared.NextDouble() < 0.5)
            throw new Exception("Simulated RuleBased failure.");

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            Method = AnalysisMethod.RuleBased,
            Score = Math.Round((decimal)(Random.Shared.NextDouble() * 100), 2),
            Feedback = "Rule-based analysis not yet implemented.",
            CompletedAt = DateTime.UtcNow
        };
    }

    private static async Task<AnalysisResult> RunKeywordAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        await Task.Delay(Random.Shared.Next(1000, 4000));

        if (Random.Shared.NextDouble() < 0.5)
            throw new Exception("Simulated Keyword failure.");

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            Method = AnalysisMethod.Keyword,
            Score = Math.Round((decimal)(Random.Shared.NextDouble() * 100), 2),
            Feedback = "Keyword analysis not yet implemented.",
            CompletedAt = DateTime.UtcNow
        };
    }
}