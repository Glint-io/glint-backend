using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;

namespace glint_backend.Interfaces;

// interface for user-specific operations
public interface IUserService
{
    // Methods for user-specific operations, e.g. retrieving analysis history and statistics.
    Task<PagedResponse<AnalysisHistoryItemResponse>> GetHistoryAsync(
        Guid userId, AnalysisHistoryRequest request);

    Task<StatisticsResponse> GetStatisticsAsync(Guid userId, AnalysisHistoryRange range);

    Task DeleteOwnAccountAsync(Guid userId, string password);
}