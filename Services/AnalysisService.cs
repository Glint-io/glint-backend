using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Interfaces;
using glint_backend.Models;

namespace glint_backend.Services;

public class AnalysisService(
    IAnalysisRepository analysisRepo,
    IResumeRepository resumeRepo,
    IJobAdvertisementRepository jobAdRepo,
    IFileValidationService fileValidator) : IAnalysisService
{
    public async Task<AnalyzeResponse> AnalyzeAsync(Guid userId, AnalyzeRequest request)
    {
        // ── 1. Validate the uploaded PDF ──────────────────────────────────────
        var validation = await fileValidator.ValidatePdfAsync(request.Resume);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);

        // ── 2. Persist resume + job ad ────────────────────────────────────────
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = request.Resume.FileName,
            FileData = validation.FileBytes!,
            UploadedAt = DateTime.UtcNow
        };
        await resumeRepo.AddAsync(resume);

        var jobAd = new JobAdvertisement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RawText = request.JobText,
            CreatedAt = DateTime.UtcNow
        };
        await jobAdRepo.AddAsync(jobAd);

        // ── 3. Create the Analysis record (InProgress) ────────────────────────
        var analysis = new Analysis
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResumeId = resume.Id,
            JobAdvertisementId = jobAd.Id,
            Label = request.Label,
            CreatedAt = DateTime.UtcNow,
            Status = AnalysisStatus.InProgress
        };
        await analysisRepo.AddAnalysisAsync(analysis);

        // ── 4. Run all three methods concurrently ─────────────────────────────
        List<AnalysisResult> results;
        try
        {
            var methods = new[]
            {
                AnalysisMethod.AI,
                AnalysisMethod.RuleBased,
                AnalysisMethod.Keyword
            };

            var tasks = methods.Select(method =>
                RunMethodAsync(analysis.Id, method, validation.FileBytes!, request.JobText));

            results = [.. await Task.WhenAll(tasks)];

            foreach (var r in results)
                await analysisRepo.AddResultAsync(r);

            analysis.Status = AnalysisStatus.Completed;
        }
        catch
        {
            analysis.Status = AnalysisStatus.Pending;
            await analysisRepo.UpdateAnalysisAsync(analysis);
            throw;
        }

        await analysisRepo.UpdateAnalysisAsync(analysis);

        // ── 5. Map to response ────────────────────────────────────────────────
        return new AnalyzeResponse
        {
            AnalysisId = analysis.Id,
            Label = analysis.Label,
            Status = analysis.Status,
            CreatedAt = analysis.CreatedAt,
            Results = results.Select(r => new AnalysisResultResponse
            {
                Id = r.Id,
                Method = r.Method.ToString(),
                Score = r.Score,
                Feedback = r.Feedback,
                CompletedAt = r.CompletedAt
            }).ToList()
        };
    }

    // ── Per-method stubs ──────────────────────────────────────────────────────
    // Replace each method body with your real implementation.
    // They are intentionally separate so they can be unit-tested independently.

    private static Task<AnalysisResult> RunMethodAsync(
        Guid analysisId, AnalysisMethod method, byte[] pdfBytes, string jobText)
    {
        return method switch
        {
            AnalysisMethod.AI => RunAiAsync(analysisId, pdfBytes, jobText),
            AnalysisMethod.RuleBased => RunRuleBasedAsync(analysisId, pdfBytes, jobText),
            AnalysisMethod.Keyword => RunKeywordAsync(analysisId, pdfBytes, jobText),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }

    private static async Task<AnalysisResult> RunAiAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        await Task.CompletedTask; // TODO: call Claude / OpenAI API

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            Method = AnalysisMethod.AI,
            Score = null,
            Feedback = "AI score not yet implemented.",
            CompletedAt = DateTime.UtcNow
        };
    }

    private static async Task<AnalysisResult> RunRuleBasedAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        await Task.CompletedTask; // TODO: implement rule-based scoring

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            Method = AnalysisMethod.RuleBased,
            Score = null,
            Feedback = "Rule-based analysis not yet implemented.",
            CompletedAt = DateTime.UtcNow
        };
    }

    private static async Task<AnalysisResult> RunKeywordAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        await Task.CompletedTask; // TODO: implement keyword extraction + match

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            Method = AnalysisMethod.Keyword,
            Score = null,
            Feedback = "Keyword analysis not yet implemented.",
            CompletedAt = DateTime.UtcNow
        };
    }
}