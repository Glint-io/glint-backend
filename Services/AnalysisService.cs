using System.Runtime.CompilerServices;
using System.Threading.Channels;
using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using glint_backend.Models;

namespace glint_backend.Services
{
    public class AnalysisService(
        IAnalysisRepository analysisRepo,
        IResumeRepository resumeRepo,
        IJobAdvertisementRepository jobAdRepo,
        IFileValidationService fileValidator,
        IAiAnalysisService aiService,
        IRuleBasedAnalysisService ruleService,
        IKeywordAnalysisService keywordService,
        IPdfExtractionService pdfExtractor) : IAnalysisService
    {
        // ── Guest (stateless) ─────────────────────────────────────────────────

        public async Task<AnalyzeResponse> AnalyzeGuestAsync(byte[] pdfBytes, JobAdvertisement jobAdvertisement)
        {
            var results = await RunAllMethodsAsync(Guid.Empty, pdfBytes, jobAdvertisement);

            // Nothing is persisted — build a transient response
            return new AnalyzeResponse
            {
                AnalysisId = Guid.Empty,
                JobTitle = null,
                Status = AnalysisStatus.Completed,
                CreatedAt = DateTime.UtcNow,
                Results = MapResults(results)
            };
        }

        // ── Authenticated ─────────────────────────────────────────────────────

        public async Task<AnalyzeResponse> AnalyzeAuthenticatedAsync(Guid userId, AnalyzeRequest request)
        {
            var (pdfBytes, resumeId) = await ResolveResumeAsync(userId, request);
            var (analysis, jobAd) = await CreateAnalysisAsync(userId, resumeId, request);

            List<AnalysisResult> results;
            try
            {
                results = await RunAllMethodsAsync(analysis.Id, pdfBytes, jobAd);

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
                JobTitle = analysis.JobTitle,
                Status = analysis.Status,
                CreatedAt = analysis.CreatedAt,
                Results = MapResults(results)
            };
        }

        // ── Individual method analysis ────────────────────────────────────────

        public async Task<AnalysisResultResponse> AnalyzeGuestMethodAsync(
            byte[] pdfBytes, JobAdvertisement jobAdvertisement, AnalysisMethod method)
        {
            var result = await RunMethodAsync(Guid.Empty, method, pdfBytes, jobAdvertisement);
            return MapResult(result);
        }

        public async Task<AnalysisResultResponse> AnalyzeAuthenticatedMethodAsync(
            Guid userId, AnalyzeRequest request, AnalysisMethod method)
        {
            var (pdfBytes, resumeId) = await ResolveResumeAsync(userId, request);
            var (analysis, jobAd) = await CreateAnalysisAsync(userId, resumeId, request);

            AnalysisResult result;
            try
            {
                result = await RunMethodAsync(analysis.Id, method, pdfBytes, jobAd);
                await analysisRepo.AddResultAsync(result);
                analysis.Status = AnalysisStatus.Completed;
            }
            catch
            {
                analysis.Status = AnalysisStatus.Pending;
                await analysisRepo.UpdateAnalysisAsync(analysis);
                throw;
            }

            await analysisRepo.UpdateAnalysisAsync(analysis);
            return MapResult(result);
        }

        // ── SSE streaming ─────────────────────────────────────────────────────

        public async IAsyncEnumerable<AnalysisStreamEvent> StreamGuestAsync(
            byte[] pdfBytes, string jobText,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var jobAd = new JobAdvertisement { RawText = jobText };
            var channel = Channel.CreateUnbounded<AnalysisStreamEvent>();
            bool isFirst = true;

            var tasks = Enum.GetValues<AnalysisMethod>().Select(async method =>
            {
                var result = await RunMethodSafeAsync(Guid.Empty, method, pdfBytes, jobAd);
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
            var (analysis, jobAd) = await CreateAnalysisAsync(userId, resumeId, request);

            var channel = Channel.CreateUnbounded<AnalysisStreamEvent>();
            bool isFirst = true;

            var tasks = Enum.GetValues<AnalysisMethod>().Select(async method =>
            {
                var result = await RunMethodSafeAsync(analysis.Id, method, pdfBytes, jobAd);
                await analysisRepo.AddResultAsync(result);

                var evt = new AnalysisStreamEvent
                {
                    AnalysisId = isFirst ? analysis.Id : null,
                    JobTitle = isFirst ? analysis.JobTitle : null,
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

        // ── Shared helpers ────────────────────────────────────────────────────

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
                    throw new NotFoundException("Saved resume not found."); // intentionally opaque

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
            var jobTitle = string.IsNullOrWhiteSpace(request.JobTitle) ? null : request.JobTitle.Trim();

            var jobAd = new JobAdvertisement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = jobTitle,
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
                JobTitle = jobTitle,
                CreatedAt = DateTime.UtcNow,
                Status = AnalysisStatus.InProgress
            };
            await analysisRepo.AddAnalysisAsync(analysis);

            return (analysis, jobAd);
        }

        private async Task<List<AnalysisResult>> RunAllMethodsAsync(
            Guid analysisId, byte[] pdfBytes, JobAdvertisement jobAdvertisement)
        {
            var tasks = Enum.GetValues<AnalysisMethod>()
                .Select(m => RunMethodSafeAsync(analysisId, m, pdfBytes, jobAdvertisement));

            return [.. await Task.WhenAll(tasks)];
        }

        // Catches any method failure and returns a 0-score result instead of throwing.
        private async Task<AnalysisResult> RunMethodSafeAsync(
            Guid analysisId, AnalysisMethod method, byte[] pdfBytes, JobAdvertisement jobAdvertisement)
        {
            try
            {
                return await RunMethodAsync(analysisId, method, pdfBytes, jobAdvertisement);
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

        private async Task<AnalysisResult> RunMethodAsync(
            Guid analysisId, AnalysisMethod method, byte[] pdfBytes, JobAdvertisement jobAdvertisement)
        {
            var doc = await pdfExtractor.ExtractAsync(pdfBytes);

            (decimal Score, string Feedback) analysisResult = method switch
            {
                AnalysisMethod.AI => await aiService.AnalyzeAsync(doc, jobAdvertisement),
                AnalysisMethod.RuleBased => await ruleService.AnalyzeAsync(doc, jobAdvertisement),
                AnalysisMethod.Keyword => await keywordService.AnalyzeAsync(doc, jobAdvertisement),
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
    }
}