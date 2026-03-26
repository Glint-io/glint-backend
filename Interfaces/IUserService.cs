using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;

namespace glint_backend.Interfaces;

public interface IUserService
{
    Task<PagedResponse<AnalysisHistoryItemResponse>> GetHistoryAsync(
        Guid userId, PaginationRequest pagination);

    Task<StatisticsResponse> GetStatisticsAsync(Guid userId);
}