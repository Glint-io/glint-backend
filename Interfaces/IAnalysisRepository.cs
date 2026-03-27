using glint_backend.Models;

namespace glint_backend.Interfaces;

public interface IAnalysisRepository
{
    
    Task<(IEnumerable<Analysis> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize);
    Task<IEnumerable<AnalysisResult>> GetResultsByUserIdAsync(Guid userId);

    // Additional methods for fetching analyses and results by various criteria can be added here, e.g. by AnalysisId, by status, etc.
    Task<Analysis> AddAnalysisAsync(Analysis analysis);
    Task<AnalysisResult> AddResultAsync(AnalysisResult result);
    Task UpdateAnalysisAsync(Analysis analysis);
}