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
    // ── Guest (stateless) ─────────────────────────────────────────────────────

    public async Task<AnalyzeResponse> AnalyzeGuestAsync(byte[] pdfBytes, string jobText)
    {
        var results = await RunAllMethodsAsync(Guid.Empty, pdfBytes, jobText);

        // Nothing is persisted — build a transient response
        return new AnalyzeResponse
        {
            AnalysisId = Guid.Empty,
            Label = null,
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Results = MapResults(results)
        };
    }

    // ── Authenticated ─────────────────────────────────────────────────────────

    public async Task<AnalyzeResponse> AnalyzeAuthenticatedAsync(Guid userId, AnalyzeRequest request)
    {
        byte[] pdfBytes;

        if (request.ResumeId.HasValue)
        {
            // Use an existing saved resume
            var saved = await resumeRepo.GetByIdAsync(request.ResumeId.Value)
                ?? throw new NotFoundException("Saved resume not found.");

            if (saved.UserId != userId)
                throw new NotFoundException("Saved resume not found."); // intentionally opaque

            pdfBytes = saved.FileData;
        }
        else
        {
            // Validate and read the uploaded PDF
            var validation = await fileValidator.ValidatePdfAsync(request.Resume!);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ErrorMessage);

            pdfBytes = validation.FileBytes!;
        }

        // get id to associate the analysis with. If new resume, create it and get the new ID. If existing resume, just use that ID.
        Guid resumeId;
        if (request.ResumeId.HasValue)
        {
            resumeId = request.ResumeId.Value;
        }
        else
        {
            var resume = new Resume
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileName = request.Resume!.FileName,
                FileData = pdfBytes,
                UploadedAt = DateTime.UtcNow
            };
            await resumeRepo.AddAsync(resume);
            resumeId = resume.Id;
        }

        // create a new jobAd for an analysis and link it to the user
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

        // run the methods concurrently
        List<AnalysisResult> results;
        try
        {
            results = await RunAllMethodsAsync(analysis.Id, pdfBytes, request.JobText);

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

        return new AnalyzeResponse
        {
            AnalysisId = analysis.Id,
            Label = analysis.Label,
            Status = analysis.Status,
            CreatedAt = analysis.CreatedAt,
            Results = MapResults(results)
        };
    }

    // Helpers for all methods

    private static async Task<List<AnalysisResult>> RunAllMethodsAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        var tasks = new[]
        {
            AnalysisMethod.AI,
            AnalysisMethod.RuleBased,
            AnalysisMethod.Keyword
        }.Select(m => RunMethodAsync(analysisId, m, pdfBytes, jobText));

        return [.. await Task.WhenAll(tasks)];
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

    private static List<AnalysisResultResponse> MapResults(List<AnalysisResult> results) =>
        results.Select(r => new AnalysisResultResponse
        {
            Id = r.Id,
            Method = r.Method.ToString(),
            Score = r.Score,
            Feedback = r.Feedback,
            CompletedAt = r.CompletedAt
        }).ToList();

    
    // not finnished methods
    private static async Task<AnalysisResult> RunAiAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        await Task.CompletedTask;
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
        await Task.CompletedTask;
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
        await Task.CompletedTask;
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