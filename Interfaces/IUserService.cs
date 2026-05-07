using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;

namespace glint_backend.Interfaces;

public interface IUserService
{
    Task<PagedResponse<AnalysisHistoryItemResponse>> GetHistoryAsync(
        Guid userId, AnalysisHistoryRequest request);

    Task<StatisticsResponse> GetStatisticsAsync(Guid userId, AnalysisHistoryRange range);

    Task<int> ClearHistoryAsync(Guid userId, AnalysisHistoryRange range);

    Task DeleteOwnAccountAsync(Guid userId, string password);
}