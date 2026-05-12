using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Exceptions;
using glint_backend.Interfaces;
using glint_backend.Repositories.Interfaces;

namespace glint_backend.Services;

public class UserService(
    IAnalysisRepository analysisRepo,
    IUserRepository userRepo) : IUserService
{
    private static DateTime? GetCreatedAtFrom(AnalysisHistoryRange range) => range switch
    {
        AnalysisHistoryRange.Today => DateTime.UtcNow.Date,
        AnalysisHistoryRange.Last7Days => DateTime.UtcNow.AddDays(-7),
        AnalysisHistoryRange.Last30Days => DateTime.UtcNow.AddDays(-30),
        AnalysisHistoryRange.Last365Days => DateTime.UtcNow.AddDays(-365),
        _ => null
    };

    public async Task<PagedResponse<AnalysisHistoryItemResponse>> GetHistoryAsync(
        Guid userId, AnalysisHistoryRequest request)
    {
        var createdAtFrom = GetCreatedAtFrom(request.Range);

        var (items, total) = await analysisRepo.GetPagedByUserIdAsync(
            userId, request.Page, request.PageSize, createdAtFrom);

        var mapped = items.Select(a => new AnalysisHistoryItemResponse
        {
            Id = a.Id,
            JobTitle = a.JobTitle,
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
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<StatisticsResponse> GetStatisticsAsync(Guid userId, AnalysisHistoryRange range)
    {
        var createdAtFrom = GetCreatedAtFrom(range);
        var results = (await analysisRepo.GetResultsByUserIdAsync(userId, createdAtFrom)).ToList();

        var totalAnalyses = results
            .Select(r => r.AnalysisId)
            .Distinct()
            .Count();

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

        var scoreOverTime = results
            .Where(r => r.Score.HasValue)
            .OrderBy(r => r.Analysis.CreatedAt)
            .Select(r => new ScoreDataPoint
            {
                Date = r.Analysis.CreatedAt,
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

    public async Task<int> ClearHistoryAsync(Guid userId, AnalysisHistoryRange range)
    {
        var createdAtFrom = GetCreatedAtFrom(range);
        return await analysisRepo.DeleteByUserIdAsync(userId, createdAtFrom);
    }

    public async Task DeleteOwnAccountAsync(Guid userId, string password)
    {
        var user = await userRepo.GetByUuidAsync(userId)
            ?? throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Incorrect password.");

        await userRepo.DeleteAsync(userId);
    }
}