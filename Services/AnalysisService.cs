using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using glint_backend.Models;
using System.Text;

namespace glint_backend.Services;

public class AnalysisService(
    IAnalysisRepository analysisRepo,
    IResumeRepository resumeRepo,
    IJobAdvertisementRepository jobAdRepo,
    IFileValidationService fileValidator,
    IAiAnalysisService aiService,
    IRuleBasedAnalysisService ruleService,
    IKeywordAnalysisService keywordService) : IAnalysisService
{
    //  Guest (stateless) 

    public async Task<AnalyzeResponse> AnalyzeGuestAsync(byte[] pdfBytes, string jobText)
    {
        var results = await RunAllMethodsAsync(Guid.Empty, pdfBytes, jobText);

        // Nothing is persisted - build a transient response
        return new AnalyzeResponse
        {
            AnalysisId = Guid.Empty,
            Label = null,
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            Results = MapResults(results)
        };
    }

    //  Authenticated 

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

        // Use provided ResumeId if available; otherwise leave ResumeId null.
        // Temporary file uploads for analysis are not persisted to the database — only saved resumes via the profile page are stored.
        Guid? resumeId = request.ResumeId;

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

    private async Task<List<AnalysisResult>> RunAllMethodsAsync(
        Guid analysisId, byte[] pdfBytes, string jobText)
    {
        var methods = new[]
        {
            AnalysisMethod.AI,
            AnalysisMethod.RuleBased,
            AnalysisMethod.Keyword
        };

        var tasks = methods.Select(m => RunMethodAsync(analysisId, m, pdfBytes, jobText));

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    private async Task<AnalysisResult> RunMethodAsync(
        Guid analysisId, AnalysisMethod method, byte[] pdfBytes, string jobText)
    {
        // Extract resume text from PDF bytes. Placeholder extraction.
        var resumeText = ExtractTextFromPdfBytes(pdfBytes);

        (decimal Score, string Feedback) analysisResult = method switch
        {
            AnalysisMethod.AI => await aiService.AnalyzeAsync(resumeText, jobText),
            AnalysisMethod.RuleBased => await ruleService.AnalyzeAsync(resumeText, jobText),
            AnalysisMethod.Keyword => await keywordService.AnalyzeAsync(resumeText, jobText),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            Method = method,
            Score = analysisResult.Score,
            Feedback = analysisResult.Feedback,
            CompletedAt = DateTime.UtcNow
        };
    }

    private static List<AnalysisResultResponse> MapResults(List<AnalysisResult> results) =>
        results.Select(r => new AnalysisResultResponse
        {
            Id = r.Id,
            Method = r.Method.ToString(),
            Score = r.Score,
            Feedback = r.Feedback,
            CompletedAt = r.CompletedAt
        }).ToList();

    private static string ExtractTextFromPdfBytes(byte[] pdfBytes)
    {
        // Placeholder: we don't have a PDF parser here. Return a simple marker including length.
        if (pdfBytes is null || pdfBytes.Length == 0)
            return string.Empty;

        return $"[PDF bytes length: {pdfBytes.Length}]";
    }

    // Individual method analysis — for dynamic results

    public async Task<AnalysisResultResponse> AnalyzeGuestMethodAsync(
        byte[] pdfBytes, string jobText, AnalysisMethod method)
    {
        var result = await RunMethodAsync(Guid.Empty, method, pdfBytes, jobText);
        return new AnalysisResultResponse
        {
            Id = result.Id,
            Method = result.Method.ToString(),
            Score = result.Score,
            Feedback = result.Feedback,
            CompletedAt = result.CompletedAt
        };
    }

    public async Task<AnalysisResultResponse> AnalyzeAuthenticatedMethodAsync(
        Guid userId, AnalyzeRequest request, AnalysisMethod method)
    {
        byte[] pdfBytes;

        if (request.ResumeId.HasValue)
        {
            var saved = await resumeRepo.GetByIdAsync(request.ResumeId.Value)
                ?? throw new NotFoundException("Saved resume not found.");

            if (saved.UserId != userId)
                throw new NotFoundException("Saved resume not found.");

            pdfBytes = saved.FileData;
        }
        else
        {
            var validation = await fileValidator.ValidatePdfAsync(request.Resume!);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ErrorMessage);

            pdfBytes = validation.FileBytes!;
        }

        Guid? resumeId = request.ResumeId;

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

        AnalysisResult result;
        try
        {
            result = await RunMethodAsync(analysis.Id, method, pdfBytes, request.JobText);
            await analysisRepo.AddResultAsync(result);
        }
        catch
        {
            analysis.Status = AnalysisStatus.Pending;
            await analysisRepo.UpdateAnalysisAsync(analysis);
            throw;
        }

        return new AnalysisResultResponse
        {
            Id = result.Id,
            Method = result.Method.ToString(),
            Score = result.Score,
            Feedback = result.Feedback,
            CompletedAt = result.CompletedAt
        };
    }
}