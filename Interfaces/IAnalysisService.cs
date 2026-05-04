using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;

namespace glint_backend.Interfaces;

public interface IAnalysisService
{
    // Standard — waits for all 3 methods, returns combined AnalyzeResponse
    Task<AnalyzeResponse> AnalyzeGuestAsync(byte[] pdfBytes, string jobText);
    Task<AnalyzeResponse> AnalyzeAuthenticatedAsync(Guid userId, AnalyzeRequest request);

    // SSE streaming — yields each AnalysisResultResponse as its method completes
    IAsyncEnumerable<AnalysisStreamEvent> StreamGuestAsync(
        byte[] pdfBytes, string jobText, CancellationToken ct = default);

    IAsyncEnumerable<AnalysisStreamEvent> StreamAuthenticatedAsync(
        Guid userId, AnalyzeRequest request, CancellationToken ct = default);
}