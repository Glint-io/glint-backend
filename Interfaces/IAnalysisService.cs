using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;

namespace glint_backend.Interfaces;

public interface IAnalysisService
{
    Task<AnalyzeResponse> AnalyzeAsync(Guid userId, AnalyzeRequest request);
}