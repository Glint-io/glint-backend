using glint_backend.DTOs.Requests;
using glint_backend.DTOs.Responses;
using glint_backend.Models;

namespace glint_backend.Interfaces;

public interface IAnalysisService
{
    // Standard � waits for all 3 methods, returns combined AnalyzeResponse
    /* Task<AnalyzeResponse> AnalyzeGuestAsync(byte[] pdfBytes, string jobText);
    Task<AnalyzeResponse> AnalyzeAuthenticatedAsync(Guid userId, AnalyzeRequest request); */

    // SSE streaming � yields each AnalysisResultResponse as its method completes
    IAsyncEnumerable<AnalysisStreamEvent> StreamGuestAsync(
        byte[] pdfBytes, string jobText, CancellationToken ct = default);

    IAsyncEnumerable<AnalysisStreamEvent> StreamAuthenticatedAsync(
        Guid userId, AnalyzeRequest request, CancellationToken ct = default);

    // For guests: must upload a PDF file and provide job text. Returns an AnalysisResponse with status "Processing".
    Task<AnalyzeResponse> AnalyzeGuestAsync(byte[] pdfBytes, JobAdvertisement jobAdvertisement);
    // For authenticated users: can either upload a PDF file or reference a previously uploaded resume by its ID.
    Task<AnalyzeResponse> AnalyzeAuthenticatedAsync(Guid userId, AnalyzeRequest request);
    // For guests: analyze a single method and return result immediately
    Task<AnalysisResultResponse> AnalyzeGuestMethodAsync(byte[] pdfBytes, JobAdvertisement jobAdvertisement, AnalysisMethod method);
    // For authenticated users: analyze a single method and return result immediately
    Task<AnalysisResultResponse> AnalyzeAuthenticatedMethodAsync(Guid userId, AnalyzeRequest request, AnalysisMethod method);
}