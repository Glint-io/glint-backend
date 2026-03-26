using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Interfaces;

namespace glint_backend.Services;

public class UserService(IAnalysisRepository analysisRepo) : IUserService
{
    public async Task<PagedResponse<AnalysisHistoryItemResponse>> GetHistoryAsync(
        Guid userId, PaginationRequest pagination)
    {
        var (items, total) = await analysisRepo.GetPagedByUserIdAsync(
            userId, pagination.Page, pagination.PageSize);

        var mapped = items.Select(a => new AnalysisHistoryItemResponse
        {
            Id = a.Id,
            Label = a.Label,
            CreatedAt = a.CreatedAt,
            Status = a.Status.ToString(),
            ResumeFileName = a.Resume?.FileName ?? string.Empty,
            JobAdSnippet = a.JobAdvertisement?.RawText.Length > 200
                ? a.JobAdvertisement.RawText[..200] + "…"
                : a.JobAdvertisement?.RawText ?? string.Empty,
            Results = a.Results.Select(r => new AnalysisResultResponse
            {
                Id = r.Id,
                Method = r.Method.ToString(),
                Score = r.Score,
                Feedback = r.Feedback,
                CompletedAt = r.CompletedAt
            }).ToList()
        }).ToList();

        return new PagedResponse<AnalysisHistoryItemResponse>
        {
            Items = mapped,
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalCount = total
        };
    }

    public async Task<StatisticsResponse> GetStatisticsAsync(Guid userId)
    {
        var results = (await analysisRepo.GetResultsByUserIdAsync(userId)).ToList();

        // Count distinct analyses that belong to this user
        var totalAnalyses = results
            .Select(r => r.AnalysisId)
            .Distinct()
            .Count();

        // Average score + count grouped by method
        var byMethod = results
            .GroupBy(r => r.Method)
            .Select(g => new MethodStatistic
            {
                Method = g.Key.ToString(),
                AverageScore = g.Where(r => r.Score.HasValue)
                                .Select(r => r.Score!.Value)
                                .DefaultIfEmpty()
                                .Average(),
                Count = g.Count()
            })
            .ToList();

        // Score over time — one data point per completed result
        var scoreOverTime = results
            .Where(r => r.Score.HasValue && r.CompletedAt.HasValue)
            .OrderBy(r => r.CompletedAt)
            .Select(r => new ScoreDataPoint
            {
                Date = r.CompletedAt!.Value,
                Score = r.Score!.Value,
                Method = r.Method.ToString()
            })
            .ToList();

        return new StatisticsResponse
        {
            TotalAnalyses = totalAnalyses,
            ByMethod = byMethod,
            ScoreOverTime = scoreOverTime
        };
    }
}